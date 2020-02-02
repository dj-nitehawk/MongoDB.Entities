using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly DataStreamer streamer;

        /// <summary>
        /// The total amount of data in bytes that has been uploaded so far
        /// </summary>
        [BsonElement]
        public double FileSize { get; internal set; }

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
        /// Access the DataStreamer class for uploading and downloading data
        /// </summary>
        public DataStreamer Data
        {
            get
            {
                return streamer ?? new DataStreamer(this);
            }
        }
    }

    public class DataStreamer
    {
        private static readonly HashSet<string> indexedDBs = new HashSet<string>();

        private readonly FileEntity parent;
        private readonly DB db;
        private FileChunk doc;
        private int chunkSize, readCount;
        private byte[] buffer;
        private List<byte> dataChunk;
        private IClientSessionHandle session;

        public DataStreamer(FileEntity parent)
        {
            this.parent = parent;
            var dbName = parent.Database();
            db = DB.GetInstance(dbName);

            if (!indexedDBs.Contains(dbName))
            {
                indexedDBs.Add(dbName);
                _ = db.Index<FileChunk>()
                      .Key(c => c.FileID, KeyType.Ascending)
                      .CreateAsync();
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
        /// <param name="cancelToken">An optional cancellation token.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public async Task DownloadAsync(Stream stream, int batchSize = 1, CancellationToken cancelToken = default, IClientSessionHandle session = null)
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

            var findTask = session == null ?
                                db.Collection<FileChunk>().FindAsync(filter, options, cancelToken) :
                                db.Collection<FileChunk>().FindAsync(session, filter, options, cancelToken);

            using (var cursor = await findTask)
            {
                var hasChunks = false;

                while (await cursor.MoveNextAsync(cancelToken))
                {
                    foreach (var chunk in cursor.Current)
                    {
                        await stream.WriteAsync(chunk, 0, chunk.Length, cancelToken);
                        hasChunks = true;
                    }
                }

                if (!hasChunks) throw new InvalidOperationException($"No data was found for file entity with ID: {parent.ID}");
            }
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
        /// <param name="cancelToken">An optional cancellation token.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public async Task UploadAsync(Stream stream, int chunkSizeKB = 256, CancellationToken cancelToken = default, IClientSessionHandle session = null)
        {
            parent.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 4096) throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
            if (!stream.CanRead) throw new NotSupportedException("The supplied stream is not readable!");
            CleanUp();

            this.session = session;
            doc = new FileChunk { FileID = parent.ID };
            chunkSize = chunkSizeKB * 1024;
            dataChunk = new List<byte>(chunkSize);
            buffer = new byte[64 * 1024]; // 64kb read buffer
            readCount = 0;

            try
            {
                if (stream.Position > 0 && stream.CanSeek) stream.Position = 0;

                while ((readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancelToken)) > 0)
                {
                    await FlushToDB();
                }

                if (parent.FileSize > 0)
                {
                    await FlushToDB(isLastChunk: true);
                    parent.UploadSuccessful = true;
                }
                else
                {
                    throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
                }
            }
            catch (Exception)
            {
                CleanUp();
                throw;
            }
            finally
            {
                await UpdateMetaData();
                doc = null;
                buffer = null;
                dataChunk = null;
            }
        }

        private void CleanUp()
        {
            _ = db.DeleteAsync<FileChunk>(c => c.FileID == parent.ID);
            parent.FileSize = 0;
            parent.ChunkCount = 0;
            parent.UploadSuccessful = false;
        }

        private async Task FlushToDB(bool isLastChunk = false)
        {
            if (!isLastChunk)
            {
                dataChunk.AddRange(
                    readCount == buffer.Length ?
                    buffer :
                    new ArraySegment<byte>(buffer, 0, readCount).ToArray());

                parent.FileSize += readCount;
            }

            if (dataChunk.Count >= chunkSize || isLastChunk)
            {
                doc.ID = null;
                doc.Data = dataChunk.ToArray();
                await db.SaveAsync(doc, session);
                parent.ChunkCount++;
                doc.Data = null;
                dataChunk.Clear();
            }
        }

        private Task UpdateMetaData()
        {
            var coll = db.Collection<FileEntity>().Database.GetCollection<FileEntity>(parent.CollectionName());

            var filter = Builders<FileEntity>.Filter.Eq(e => e.ID, parent.ID);
            var update = Builders<FileEntity>.Update
                            .Set(e => e.FileSize, parent.FileSize)
                            .Set(e => e.ChunkCount, parent.ChunkCount)
                            .Set(e => e.UploadSuccessful, parent.UploadSuccessful);

            return session == null
                   ? coll.UpdateOneAsync(filter, update)
                   : coll.UpdateOneAsync(session, filter, update);
        }
    }

    [Name("[BINARY_CHUNKS]")]
    internal class FileChunk : IEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }

        [Ignore]
        public DateTime ModifiedOn { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string FileID { get; set; }

        public byte[] Data { get; set; }
    }
}
