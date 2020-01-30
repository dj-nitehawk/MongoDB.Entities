using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities.Utilities
{
    //todo: delete chunks when entity gets deleted

    public abstract class FileEntity : Entity
    {
        public double FileSize { get; set; }
        public int ChunkCount { get; set; }
        public bool UploadSuccessful { get; set; }

        private DB db;
        private FileChunk doc;
        private int chunkSize, readCount;
        private byte[] buffer;
        private CancellationToken cancellation;
        private IClientSessionHandle session;

        public async Task UploadDataAsync(Stream stream, int chunkSizeKB = 256, CancellationToken cancellation = default, IClientSessionHandle session = null)
        {
            this.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 1024) throw new ArgumentException("Please specify a chunk size from 128KB to 1024KB");

            this.cancellation = cancellation;
            this.session = session;
            db = DB.GetInstance(this.Database());
            doc = new FileChunk { FileID = ID };
            chunkSize = chunkSizeKB * 1024;
            buffer = new byte[4096]; // 4kb buffer
            readCount = 0;

            try
            {
                while ((readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation)) > 0)
                {
                    await FlushToDB();
                }
                await FlushToDB(isLastChunk: true);
                UploadSuccessful = true;
            }
            catch (Exception)
            {
                _ = db.DeleteAsync<FileChunk>(c => c.FileID == this.ID);
                FileSize = 0;
                ChunkCount = 0;
                UploadSuccessful = false;
                throw;
            }
            finally
            {
                stream.Close();
                await UpdateMetaData();
                doc = null;
                buffer = null;
            }
        }

        private async Task FlushToDB(bool isLastChunk = false)
        {
            if (doc.Data == null)
                doc.Data = new List<byte>(chunkSize);

            if (!isLastChunk)
            {
                doc.Data.AddRange(
                    readCount == buffer.Length ?
                    buffer :
                    new ArraySegment<byte>(buffer, 0, readCount).ToArray());
            }

            if (doc.Data.Count >= chunkSize || isLastChunk)
            {
                doc.ID = null;
                await db.SaveAsync(doc, session);
                ChunkCount++;
                FileSize += doc.Data.Count;
                doc.Data = null;
            }
        }

        private async Task UpdateMetaData()
        {
            string dbName = null, collName = null;

            var type = this.GetType();
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
    internal class FileChunk : Entity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileID { get; set; }

        public List<byte> Data { get; set; }// = new List<byte>();
    }
}
