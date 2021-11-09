using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("MongoDB.Entities.Tests")]
namespace MongoDB.Entities
{
    /// <summary>
    /// Inherit this base class in order to create your own File Entities
    /// </summary>
    public abstract class FileEntity : Entity
    {
        /// <summary>
        /// The total amount of data in bytes that has been uploaded so far
        /// </summary>
        [BsonElement]
        public long FileSize { get; internal set; }

        /// <summary>
        /// The number of chunks that have been created so far
        /// </summary>
        [BsonElement]
        public int ChunkCount { get; internal set; }

        /// <summary>
        /// Returns true only when all the chunks have been stored successfully in mongodb
        /// </summary>
        [BsonElement]
        public bool UploadSuccessful { get; internal set; }

        /// <summary>
        /// If this value is set, the uploaded data will be hashed and matched against this value. If the hash is not equal, an exception will be thrown by the UploadAsync() method.
        /// </summary>
        [IgnoreDefault]
        public string? MD5 { get; set; }
    }

    [Collection("[BINARY_CHUNKS]")]
    internal class FileChunk : IEntity
    {
        [BsonId, ObjectId]
        public string ID { get; set; } = null!;

        [AsObjectId]
        public string FileID { get; set; } = null!;

        public byte[] Data { get; set; } = Array.Empty<byte>();

        public string GenerateNewID()
            => ObjectId.GenerateNewId().ToString();
    }

    /// <summary>
    /// Provides the interface for uploading and downloading data chunks for file entities.
    /// </summary>
    public class DataStreamer<T> where T : FileEntity
    {
        private static readonly HashSet<string> _indexedDBs = new();

        private readonly T _parent;
        private readonly DBContext _db;
        private readonly IMongoCollection<FileChunk> _chunkCollection;
        private FileChunk? _doc;
        private int _chunkSize, _readCount;
        private byte[]? _buffer;
        private List<byte>? _dataChunk;
        private MD5? _md5;

        internal DataStreamer(T parent, DBContext db)
        {
            _parent = parent;
            _db = db;
            _chunkCollection = db.CollectionFor<FileChunk>();

            if (_indexedDBs.Add(db.DatabaseNamespace.DatabaseName))
            {
                _ = _chunkCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<FileChunk>(
                        Builders<FileChunk>.IndexKeys.Ascending(c => c.FileID),
                        new CreateIndexOptions { Background = true, Name = $"{nameof(FileChunk.FileID)}(Asc)" }));
            }
        }

        /// <summary>
        /// Download binary data for this file entity from mongodb in chunks into a given stream with a timeout period.
        /// </summary>
        /// <param name="stream">The output stream to write the data</param>
        /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>
        /// <param name="batchSize"></param>
        public Task DownloadWithTimeoutAsync(Stream stream, int timeOutSeconds, int batchSize = 1)
        {
            return DownloadAsync(stream, batchSize, new CancellationTokenSource(timeOutSeconds * 1000).Token);
        }

        /// <summary>
        /// Download binary data for this file entity from mongodb in chunks into a given stream.
        /// </summary>
        /// <param name="stream">The output stream to write the data</param>
        /// <param name="batchSize">The number of chunks you want returned at once</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        public async Task DownloadAsync(Stream stream, int batchSize = 1, CancellationToken cancellation = default)
        {
            _parent.ThrowIfUnsaved();
            if (!_parent.UploadSuccessful) throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");
            if (!stream.CanWrite) throw new NotSupportedException("The supplied stream is not writable!");

            var filter = Builders<FileChunk>.Filter.Eq(c => c.FileID, _parent.ID);
            var options = new FindOptions<FileChunk, byte[]>
            {
                BatchSize = batchSize,
                Sort = Builders<FileChunk>.Sort.Ascending(c => c.ID),
                Projection = Builders<FileChunk>.Projection.Expression(c => c.Data)
            };

            var findTask =
                _db.Session is not IClientSessionHandle session
                ? _chunkCollection.FindAsync(filter, options, cancellation)
                : _chunkCollection.FindAsync(session, filter, options, cancellation);

            using var cursor = await findTask.ConfigureAwait(false);
            var hasChunks = false;

            while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                foreach (var chunk in cursor.Current)
                {
                    await stream.WriteAsync(chunk, 0, chunk.Length, cancellation).ConfigureAwait(false);
                    hasChunks = true;
                }
            }

            if (!hasChunks) throw new InvalidOperationException($"No data was found for file entity with ID: {_parent.ID}");
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream with a timeout period.
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        public Task UploadWithTimeoutAsync(Stream stream, int timeOutSeconds, int chunkSizeKB = 256)
        {
            return UploadAsync(stream, chunkSizeKB, new CancellationTokenSource(timeOutSeconds * 1000).Token);
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream.
        /// <para>TIP: Make sure to save the entity before calling this method.</para>
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        public async Task UploadAsync(Stream stream, int chunkSizeKB = 256, CancellationToken cancellation = default)
        {
            _parent.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 4096) throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
            if (!stream.CanRead) throw new NotSupportedException("The supplied stream is not readable!");
            await CleanUpAsync().ConfigureAwait(false);

            _doc = new FileChunk { FileID = _parent.ID };
            _chunkSize = chunkSizeKB * 1024;
            _dataChunk = new List<byte>(_chunkSize);
            _buffer = new byte[64 * 1024]; // 64kb read buffer
            _readCount = 0;

            if (!string.IsNullOrEmpty(_parent.MD5))
                _md5 = MD5.Create();

            try
            {
                if (stream.CanSeek && stream.Position > 0) stream.Position = 0;

                while ((_readCount = await stream.ReadAsync(_buffer, 0, _buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                {
                    _md5?.TransformBlock(_buffer, 0, _readCount, null, 0);
                    await FlushToDBAsync(isLastChunk: false, cancellation).ConfigureAwait(false);
                }

                if (_parent.FileSize > 0)
                {
                    _md5?.TransformFinalBlock(_buffer, 0, _readCount);
                    if (_md5 != null && !BitConverter.ToString(_md5.Hash).Replace("-", "").Equals(_parent.MD5, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("MD5 of uploaded data doesn't match with file entity MD5.");
                    }
                    await FlushToDBAsync(isLastChunk: true, cancellation).ConfigureAwait(false);
                    _parent.UploadSuccessful = true;
                }
                else
                {
                    throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
                }
            }
            catch (Exception)
            {
                await CleanUpAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await UpdateMetaDataAsync().ConfigureAwait(false);
                _doc = null;
                _buffer = null;
                _dataChunk = null;
                _md5?.Dispose();
                _md5 = null;
            }
        }

        /// <summary>
        /// Deletes only the binary chunks stored in the database for this file entity.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token.</param>
        public Task DeleteBinaryChunks(CancellationToken cancellation = default)
        {
            _parent.ThrowIfUnsaved();

            if (cancellation != default && _db.Session == null)
                throw new NotSupportedException("Cancellation is only supported within transactions for deleting binary chunks!");

            return CleanUpAsync(cancellation);
        }

        private Task CleanUpAsync(CancellationToken cancellation = default)
        {
            _parent.FileSize = 0;
            _parent.ChunkCount = 0;
            _parent.UploadSuccessful = false;
            return _db.Session is not IClientSessionHandle session
                   ? _chunkCollection.DeleteManyAsync(c => c.FileID == _parent.ID, cancellation)
                   : _chunkCollection.DeleteManyAsync(session, c => c.FileID == _parent.ID, null, cancellation);
        }

        private Task FlushToDBAsync(bool isLastChunk = false, CancellationToken cancellation = default)
        {
            if (!isLastChunk)
            {
                _dataChunk?.AddRange(new ArraySegment<byte>(_buffer, 0, _readCount));
                _parent.FileSize += _readCount;
            }
            if (_doc is null)
            {
                return Task.CompletedTask;
            }
            if (_dataChunk is not null && (_dataChunk.Count >= _chunkSize || isLastChunk))
            {

                _doc.ID = _doc.GenerateNewID();
                _doc.Data = _dataChunk.ToArray();
                _dataChunk.Clear();
                _parent.ChunkCount++;
                return _db.Session is not IClientSessionHandle session
                       ? _chunkCollection.InsertOneAsync(_doc, null, cancellation)
                       : _chunkCollection.InsertOneAsync(session, _doc, null, cancellation);
            }

            return Task.CompletedTask;
        }

        private Task UpdateMetaDataAsync()
        {
            var collection = _db.CollectionFor<T>();
            var filter = Builders<T>.Filter.Eq(e => e.ID, _parent.ID);
            var update = Builders<T>.Update
                            .Set(e => e.FileSize, _parent.FileSize)
                            .Set(e => e.ChunkCount, _parent.ChunkCount)
                            .Set(e => e.UploadSuccessful, _parent.UploadSuccessful);

            return _db.Session is not IClientSessionHandle session
                   ? collection.UpdateOneAsync(filter, update)
                   : collection.UpdateOneAsync(session, filter, update);
        }
    }
}
