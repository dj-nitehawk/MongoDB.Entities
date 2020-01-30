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
        public double FileSize { get; set; }
        public int ChunkCount { get; set; }
        public bool UploadSuccessful { get; set; }

        private string dbName = null, collName = null;
        private DB db;
        private FileChunk doc;
        private int chunkSize, readCount;
        private byte[] buffer;
        private List<byte> currentChunk;
        private IClientSessionHandle session;

        private void Init()
        {
            db = DB.GetInstance(this.Database());

            var type = GetType();
            collName = type.Name;

            var nameAttribs = (NameAttribute[])type.GetCustomAttributes(typeof(NameAttribute), false);
            if (nameAttribs.Length > 0)
            {
                collName = nameAttribs[0].Name;
            }

            var dbAttribs = (DatabaseAttribute[])type.GetCustomAttributes(typeof(DatabaseAttribute), false);
            if (dbAttribs.Length > 0)
            {
                dbName = dbAttribs[0].Name;
            }
        }

        public async Task DownloadDataAsync(Stream stream, int batchSize = 1, CancellationTokenSource cancellation = null, IClientSessionHandle session = null)
        {
            this.ThrowIfUnsaved();
            if (!UploadSuccessful) throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");
            if (!stream.CanWrite) throw new NotSupportedException("The supplied stream is not writable!");

            Init();
            cancellation = cancellation ?? new CancellationTokenSource(30 * 1000);

            var filter = Builders<FileChunk>.Filter.Eq(c => c.FileID, ID);
            var options = new FindOptions<FileChunk, byte[]>
            {
                BatchSize = batchSize,
                Projection = Builders<FileChunk>.Projection.Expression(c => c.Data)
            };

            try
            {
                var findTask = session == null ?
                                db.Collection<FileChunk>().FindAsync(filter, options, cancellation.Token) :
                                db.Collection<FileChunk>().FindAsync(session, filter, options, cancellation.Token);

                using (var cursor = await findTask)
                {
                    while (await cursor.MoveNextAsync(cancellation.Token))
                    {
                        foreach (var chunk in cursor.Current)
                        {
                            await stream.WriteAsync(chunk, 0, chunk.Length, cancellation.Token);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream.
        /// <para>TIP: Make sure to save the entity before calling this method.</para>
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        /// <param name="cancellation">A cancellation token source. You can create one with new CancellationTokenSource(TimeoutSeconds)</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public async Task UploadDataAsync(Stream stream, int chunkSizeKB = 256, CancellationTokenSource cancellation = null, IClientSessionHandle session = null)
        {
            this.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 4096) throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
            if (!stream.CanRead) throw new NotSupportedException("The supplied stream is not readable!");
            Init();
            CleanUp();

            this.session = session;
            cancellation = cancellation ?? new CancellationTokenSource(30 * 1000);
            doc = new FileChunk { FileID = ID };
            chunkSize = chunkSizeKB * 1024;
            buffer = new byte[64 * 1024]; // 64kb buffer
            readCount = 0;

            try
            {
                while ((readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation.Token)) > 0)
                {
                    await FlushToDB();
                }

                if (FileSize > 0)
                {
                    await FlushToDB(isLastChunk: true);
                    UploadSuccessful = true;
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
                currentChunk = null;
            }
        }

        private void CleanUp()
        {
            _ = db.DeleteAsync<FileChunk>(c => c.FileID == ID);
            FileSize = 0;
            ChunkCount = 0;
            UploadSuccessful = false;
        }

        private async Task FlushToDB(bool isLastChunk = false)
        {
            if (currentChunk == null)
                currentChunk = new List<byte>(chunkSize);

            if (!isLastChunk)
            {
                currentChunk.AddRange(
                    readCount == buffer.Length ?
                    buffer :
                    new ArraySegment<byte>(buffer, 0, readCount).ToArray());
            }

            if (currentChunk.Count >= chunkSize || isLastChunk)
            {
                doc.ID = null;
                doc.Data = currentChunk.ToArray();
                await db.SaveAsync(doc, session);
                ChunkCount++;
                FileSize += currentChunk.Count;
                doc.Data = null;
                currentChunk = null;
            }
        }

        private async Task UpdateMetaData()
        {
            _ = DB.Index<FileChunk>(dbName)
                  .Key(c => c.FileID, KeyType.Ascending)
                  .CreateAsync();

            var coll = DB.Collection<FileEntity>(dbName).Database.GetCollection<FileEntity>(collName);

            var filter = Builders<FileEntity>.Filter.Eq(e => e.ID, ID);
            var update = Builders<FileEntity>.Update
                            .Set(e => e.FileSize, FileSize)
                            .Set(e => e.ChunkCount, ChunkCount)
                            .Set(e => e.UploadSuccessful, UploadSuccessful);

            await (session == null ?
                coll.UpdateOneAsync(filter, update) :
                coll.UpdateOneAsync(session, filter, update));
        }
    }

    [Name("_FILE_CHUNKS_")]
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
