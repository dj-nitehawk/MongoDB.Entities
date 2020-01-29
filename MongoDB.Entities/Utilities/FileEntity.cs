using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.IO;
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

        public async Task UploadDataAsync(Stream stream, int chunkSizeKB = 256, CancellationTokenSource cancellation = null, IClientSessionHandle session = null)
        {
            this.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 1024) throw new ArgumentException("Please specify a chunk size from 128KB to 1024KB");

            var db = DB.GetInstance(this.Database());
            var buffer = new byte[chunkSizeKB * 1024];
            var bytesRead = 0;
            var chunkDoc = new FileChunk();
            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    cancellation?.Token.ThrowIfCancellationRequested();

                    await db.SaveAsync(new FileChunk { FileID = this.ID, Data = buffer }, session);
                    FileSize += bytesRead;
                    ChunkCount++;
                }
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
                await UpdateMetaData(session);
            }
        }

        private async Task UpdateMetaData(IClientSessionHandle session = null)
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

            _ = await (session == null ?
                    coll.UpdateOneAsync(filter, update) :
                    coll.UpdateOneAsync(session, filter, update));
        }
    }

    [Name("_FILE_CHUNKS_")]
    internal class FileChunk : Entity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileID { get; set; }

        public byte[] Data { get; set; }
    }
}
