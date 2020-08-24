using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class FileEntities
    {
        private const string dbName = "mongodb-entities-test-multi";

        [TestCategory("SkipWhenLiveUnitTesting")]
        public async Task uploading_data_from_http_stream()
        {
            DB.Init(dbName);

            var img = new Image { Height = 800, Width = 600, Name = "Test.Png" };
            await img.SaveAsync().ConfigureAwait(false);

            //https://placekitten.com/g/4000/4000 - 1097221
            //https://djnitehawk.com/test/test.bmp - 69455612
            using var stream = await new System.Net.Http.HttpClient().GetStreamAsync("https://djnitehawk.com/test/test.bmp").ConfigureAwait(false);
            await img.Data.UploadWithTimeoutAsync(stream, 30, 128).ConfigureAwait(false);

            var count = DB.Database(dbName).GetCollection<FileChunk>(DB.CollectionName<FileChunk>()).AsQueryable()
                          .Where(c => c.FileID == img.ID)
                          .Count();

            Assert.AreEqual(1097221, img.FileSize);
            Assert.AreEqual(img.ChunkCount, count);
        }

        [TestMethod]
        public async Task uploading_data_from_file_stream()
        {
            DB.Init(dbName);

            var img = new Image { Height = 800, Width = 600, Name = "Test.Png" };
            await img.SaveAsync().ConfigureAwait(false);

            using var stream = File.OpenRead("Models/test.jpg");
            await img.Data.UploadAsync(stream).ConfigureAwait(false);

            var count = DB.Database(dbName).GetCollection<FileChunk>(DB.CollectionName<FileChunk>()).AsQueryable()
                          .Where(c => c.FileID == img.ID)
                          .Count();

            Assert.AreEqual(2047524, img.FileSize);
            Assert.AreEqual(img.ChunkCount, count);
        }

        [TestMethod]
        public async Task file_smaller_than_chunk_size()
        {
            DB.Init(dbName);

            var img = new Image { Height = 100, Width = 100, Name = "Test-small.Png" };
            await img.SaveAsync().ConfigureAwait(false);

            using var stream = File.OpenRead("Models/test.jpg");
            await img.Data.UploadAsync(stream, 4096).ConfigureAwait(false);

            var count = DB.Database(dbName).GetCollection<FileChunk>(DB.CollectionName<FileChunk>()).AsQueryable()
                          .Where(c => c.FileID == img.ID)
                          .Count();

            Assert.AreEqual(2047524, img.FileSize);
            Assert.AreEqual(img.ChunkCount, count);
        }

        [TestMethod]
        public async Task deleting_entity_deletes_all_chunks()
        {
            DB.Init(dbName);

            var img = new Image { Height = 400, Width = 400, Name = "Test-Delete.Png" };
            await img.SaveAsync().ConfigureAwait(false);

            using var stream = File.Open("Models/test.jpg", FileMode.Open);
            await img.Data.UploadAsync(stream).ConfigureAwait(false);

            var countBefore =
                DB.Database(dbName).GetCollection<FileChunk>(DB.CollectionName<FileChunk>()).AsQueryable()
                  .Where(c => c.FileID == img.ID)
                  .Count();

            Assert.AreEqual(img.ChunkCount, countBefore);

            img.Delete();

            var countAfter =
                DB.Database(dbName).GetCollection<FileChunk>(DB.CollectionName<FileChunk>()).AsQueryable()
                  .Where(c => c.FileID == img.ID)
                  .Count();

            Assert.AreEqual(0, countAfter);
        }

        [TestMethod]
        public async Task downloading_file_chunks_works()
        {
            DB.Init(dbName);

            var img = new Image { Height = 500, Width = 500, Name = "Test-Download.Png" };
            await img.SaveAsync().ConfigureAwait(false);

            using (var inStream = File.OpenRead("Models/test.jpg"))
            {
                await img.Data.UploadAsync(inStream).ConfigureAwait(false);
            }

            using (var outStream = File.OpenWrite("Models/result.jpg"))
            {
                await img.Data.DownloadAsync(outStream, 3).ConfigureAwait(false);
            }

            using var md5 = MD5.Create();
            var oldHash = md5.ComputeHash(File.OpenRead("Models/test.jpg"));
            var newHash = md5.ComputeHash(File.OpenRead("Models/result.jpg"));

            Assert.IsTrue(oldHash.SequenceEqual(newHash));
        }

        [TestMethod]
        public async Task downloading_file_chunks_directly()
        {
            DB.Init(dbName);

            var img = new Image { Height = 500, Width = 500, Name = "Test-Download.Png" };
            await img.SaveAsync().ConfigureAwait(false);

            using (var inStream = File.OpenRead("Models/test.jpg"))
            {
                await img.Data.UploadAsync(inStream).ConfigureAwait(false);
            }

            using (var outStream = File.OpenWrite("Models/result-direct.jpg"))
            {
                await DB.File<Image>(img.ID).DownloadAsync(outStream).ConfigureAwait(false);
            }

            using var md5 = MD5.Create();
            var oldHash = md5.ComputeHash(File.OpenRead("Models/test.jpg"));
            var newHash = md5.ComputeHash(File.OpenRead("Models/result-direct.jpg"));

            Assert.IsTrue(oldHash.SequenceEqual(newHash));
        }

        [TestMethod]
        public Task trying_to_download_when_no_chunks_present()
        {
            DB.Init(dbName);

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    using var stream = File.OpenWrite("test.file");
                    DB.File<Image>(ObjectId.GenerateNewId().ToString())
                      .DownloadAsync(stream).GetAwaiter().GetResult();
                });

            return Task.CompletedTask;
        }
    }
}
