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
        private DataStreamer streamer;

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
        public string MD5 { get; set; }

        [IgnoreDefault]
        public string TenantPrefix { get; set; }

        /// <summary>
        /// Access the DataStreamer class for uploading and downloading data
        /// </summary>
        public DataStreamer Data => streamer ??= new DataStreamer(this, TenantPrefix);
    }

    [Collection("[BINARY_CHUNKS]")]
    internal class FileChunk : IEntity
    {
        [BsonId, ObjectId]
        public string ID { get; set; }

        [AsObjectId]
        public string FileID { get; set; }

        public byte[] Data { get; set; }

        public string GenerateNewID()
            => ObjectId.GenerateNewId().ToString();
    }

    /// <summary>
    /// Provides the interface for uploading and downloading data chunks for file entities.
    /// </summary>
    public class DataStreamer
    {
        private static readonly HashSet<string> indexedDBs = new();

        private readonly FileEntity parent;
        private readonly Type parentType;
        private readonly IMongoDatabase db;
        private readonly IMongoCollection<FileChunk> chunkCollection;
        private FileChunk doc;
        private int chunkSize, readCount;
        private byte[] buffer;
        private List<byte> dataChunk;
        private MD5 md5;

        internal DataStreamer(FileEntity parent, string tenantPrefix)
        {
            this.parent = parent;
            parentType = parent.GetType();
            db = DB.Database(Cache.GetFullDbName(parentType, tenantPrefix));
            chunkCollection = db.GetCollection<FileChunk>(DB.CollectionName<FileChunk>());

            if (indexedDBs.Add(db.DatabaseNamespace.DatabaseName))
            {
                _ = chunkCollection.Indexes.CreateOneAsync(
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
        /// <param name="session"></param>
        public Task DownloadWithTimeoutAsync(Stream stream, int timeOutSeconds, int batchSize = 1, IClientSessionHandle session = null)
        {
            return DownloadAsync(stream, batchSize, new CancellationTokenSource(timeOutSeconds * 1000).Token, session);
        }

        /// <summary>
        /// Download binary data for this file entity from mongodb in chunks into a given stream.
        /// </summary>
        /// <param name="stream">The output stream to write the data</param>
        /// <param name="batchSize">The number of chunks you want returned at once</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public async Task DownloadAsync(Stream stream, int batchSize = 1, CancellationToken cancellation = default, IClientSessionHandle session = null)
        {
            parent.ThrowIfUnsaved();
            if (!parent.UploadSuccessful) throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");
            if (!stream.CanWrite) throw new NotSupportedException("The supplied stream is not writable!");

            var filter = Builders<FileChunk>.Filter.Eq(c => c.FileID, parent.ID);
            var options = new FindOptions<FileChunk, byte[]>
            {
                BatchSize = batchSize,
                Sort = Builders<FileChunk>.Sort.Ascending(c => c.ID),
                Projection = Builders<FileChunk>.Projection.Expression(c => c.Data)
            };

            var findTask =
                session == null
                ? chunkCollection.FindAsync(filter, options, cancellation)
                : chunkCollection.FindAsync(session, filter, options, cancellation);

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

            if (!hasChunks) throw new InvalidOperationException($"No data was found for file entity with ID: {parent.ID}");
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream with a timeout period.
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public Task UploadWithTimeoutAsync(Stream stream, int timeOutSeconds, int chunkSizeKB = 256, IClientSessionHandle session = null)
        {
            return UploadAsync(stream, chunkSizeKB, new CancellationTokenSource(timeOutSeconds * 1000).Token, session);
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream.
        /// <para>TIP: Make sure to save the entity before calling this method.</para>
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public async Task UploadAsync(Stream stream, int chunkSizeKB = 256, CancellationToken cancellation = default, IClientSessionHandle session = null)
        {
            parent.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 4096) throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
            if (!stream.CanRead) throw new NotSupportedException("The supplied stream is not readable!");
            await CleanUpAsync(session).ConfigureAwait(false);

            doc = new FileChunk { FileID = parent.ID };
            chunkSize = chunkSizeKB * 1024;
            dataChunk = new List<byte>(chunkSize);
            buffer = new byte[64 * 1024]; // 64kb read buffer
            readCount = 0;

            if (!string.IsNullOrEmpty(parent.MD5))
                md5 = MD5.Create();

            try
            {
                if (stream.CanSeek && stream.Position > 0) stream.Position = 0;

                while ((readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                {
                    md5?.TransformBlock(buffer, 0, readCount, null, 0);
                    await FlushToDBAsync(session, isLastChunk: false, cancellation).ConfigureAwait(false);
                }

                if (parent.FileSize > 0)
                {
                    md5?.TransformFinalBlock(buffer, 0, readCount);
                    if (md5 != null && !BitConverter.ToString(md5.Hash).Replace("-", "").Equals(parent.MD5, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("MD5 of uploaded data doesn't match with file entity MD5.");
                    }
                    await FlushToDBAsync(session, isLastChunk: true, cancellation).ConfigureAwait(false);
                    parent.UploadSuccessful = true;
                }
                else
                {
                    throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
                }
            }
            catch (Exception)
            {
                await CleanUpAsync(session).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await UpdateMetaDataAsync(session).ConfigureAwait(false);
                doc = null;
                buffer = null;
                dataChunk = null;
                md5?.Dispose();
                md5 = null;
            }
        }

        /// <summary>
        /// Deletes only the binary chunks stored in the database for this file entity.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        public Task DeleteBinaryChunks(IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            parent.ThrowIfUnsaved();

            if (cancellation != default && session == null)
                throw new NotSupportedException("Cancellation is only supported within transactions for deleting binary chunks!");

            return CleanUpAsync(session, cancellation);
        }

        private Task CleanUpAsync(IClientSessionHandle session, CancellationToken cancellation = default)
        {
            parent.FileSize = 0;
            parent.ChunkCount = 0;
            parent.UploadSuccessful = false;
            return session == null
                   ? chunkCollection.DeleteManyAsync(c => c.FileID == parent.ID, cancellation)
                   : chunkCollection.DeleteManyAsync(session, c => c.FileID == parent.ID, null, cancellation);
        }

        private Task FlushToDBAsync(IClientSessionHandle session, bool isLastChunk = false, CancellationToken cancellation = default)
        {
            if (!isLastChunk)
            {
                dataChunk.AddRange(new ArraySegment<byte>(buffer, 0, readCount));
                parent.FileSize += readCount;
            }

            if (dataChunk.Count >= chunkSize || isLastChunk)
            {
                doc.ID = doc.GenerateNewID();
                doc.Data = dataChunk.ToArray();
                dataChunk.Clear();
                parent.ChunkCount++;
                return session == null
                       ? chunkCollection.InsertOneAsync(doc, null, cancellation)
                       : chunkCollection.InsertOneAsync(session, doc, null, cancellation);
            }

            return Task.CompletedTask;
        }

        private Task UpdateMetaDataAsync(IClientSessionHandle session)
        {
            var collection = db.GetCollection<FileEntity>(Cache.CollectionNameFor(parentType));
            var filter = Builders<FileEntity>.Filter.Eq(e => e.ID, parent.ID);
            var update = Builders<FileEntity>.Update
                            .Set(e => e.FileSize, parent.FileSize)
                            .Set(e => e.ChunkCount, parent.ChunkCount)
                            .Set(e => e.UploadSuccessful, parent.UploadSuccessful);

            return session == null
                   ? collection.UpdateOneAsync(filter, update)
                   : collection.UpdateOneAsync(session, filter, update);
        }
    }
}
