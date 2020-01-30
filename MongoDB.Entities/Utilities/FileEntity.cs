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
namespace MongoDB.Entities.Utilities
{
    //todo: delete chunks when entity gets deleted

    /// <summary>
    /// Inherit this base class in order to create your own File Entities
    /// </summary>
    public abstract class FileEntity : Entity
    {
        public double FileSize { get; set; }
        public int ChunkCount { get; set; }
        public bool UploadSuccessful { get; set; }

        private DB db;
        private FileChunk doc;
        private int chunkSize, readCount;
        private byte[] buffer;
        private List<byte> currentChunk;
        private IClientSessionHandle session;

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream.
        /// <para>TIP: Make sure to save the entity before calling this method.</para>
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        /// <param name="cancellation">A cancellation token source. You can create one with new CancellationTokenSource(Timeout)</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns></returns>
        public async Task UploadDataAsync(Stream stream, int chunkSizeKB = 256, CancellationTokenSource cancellation = null, IClientSessionHandle session = null)
        {
            this.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 4096) throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");

            db = DB.GetInstance(this.Database());
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
                await FlushToDB(isLastChunk: true);
                UploadSuccessful = true;
            }
            catch (Exception)
            {
                CleanUp();
                throw;
            }
            finally
            {
                stream.Close();
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
            string dbName = null, collName = null;

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
