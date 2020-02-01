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
        [TestMethod]
        public async Task uploading_data_works()
        {
            var db = new DB("mongodb-entities-test-multi");

            var img = new Image { Height = 800, Width = 600, Name = "Test.Png" };
            await img.SaveAsync();

            //https://placekitten.com/g/4000/4000
            //https://djnitehawk.com/test/test.bmp
            //using var stream = await new System.Net.Http.HttpClient().GetStreamAsync("https://placekitten.com/g/4000/4000");
            //await img.Data.UploadWithTimeoutAsync(stream, 10, 4096);

            using var stream = File.OpenRead("Models/test.jpg");
            await img.Data.UploadAsync(stream);

            var count = db.Queryable<FileChunk>()
                          .Where(c => c.FileID == img.ID)
                          .Count();

            //Assert.AreEqual(613650, img.FileSize);
            Assert.AreEqual(2047524, img.FileSize);
            Assert.AreEqual(img.ChunkCount, count);
        }

        [TestMethod]
        public async Task file_smaller_than_chunk_size()
        {
            var db = new DB("mongodb-entities-test-multi");

            var img = new Image { Height = 100, Width = 100, Name = "Test-small.Png" };
            await img.SaveAsync();

            using var stream = File.OpenRead("Models/test.jpg");
            await img.Data.UploadAsync(stream, 4096);

            var count = db.Queryable<FileChunk>()
                          .Where(c => c.FileID == img.ID)
                          .Count();

            Assert.AreEqual(2047524, img.FileSize);
            Assert.AreEqual(img.ChunkCount, count);
        }

        [TestMethod]
        public async Task deleting_entity_deletes_all_chunks()
        {
            var db = new DB("mongodb-entities-test-multi");

            var img = new Image { Height = 400, Width = 400, Name = "Test-Delete.Png" };
            await img.SaveAsync();

            using var stream = File.Open("Models/test.jpg", FileMode.Open);
            await img.Data.UploadAsync(stream);

            var countBefore =
                db.Queryable<FileChunk>()
                  .Where(c => c.FileID == img.ID)
                  .Count();

            Assert.AreEqual(img.ChunkCount, countBefore);

            img.Delete();

            var countAfter =
                db.Queryable<FileChunk>()
                  .Where(c => c.FileID == img.ID)
                  .Count();

            Assert.AreEqual(0, countAfter);
        }

        [TestMethod]
        public async Task downloading_file_chunks_works()
        {
            new DB("mongodb-entities-test-multi");

            var img = new Image { Height = 500, Width = 500, Name = "Test-Download.Png" };
            await img.SaveAsync();

            using (var inStream = File.OpenRead("Models/test.jpg"))
            {
                await img.Data.UploadAsync(inStream);
            }

            using (var outStream = File.OpenWrite("Models/result.jpg"))
            {
                await img.Data.DownloadAsync(outStream, 3);
            }

            using (var md5 = MD5.Create())
            {
                var oldHash = md5.ComputeHash(File.OpenRead("Models/test.jpg"));
                var newHash = md5.ComputeHash(File.OpenRead("Models/result.jpg"));

                Assert.IsTrue(oldHash.SequenceEqual(newHash));
            }
        }

        [TestMethod]
        public async Task downloading_file_chunks_directly()
        {
            new DB("mongodb-entities-test-multi");

            var img = new Image { Height = 500, Width = 500, Name = "Test-Download.Png" };
            await img.SaveAsync();

            using (var inStream = File.OpenRead("Models/test.jpg"))
            {
                await img.Data.UploadAsync(inStream);
            }

            using (var outStream = File.OpenWrite("Models/result-direct.jpg"))
            {
                await DB.File<Image>(img.ID).DownloadAsync(outStream);
            }

            using (var md5 = MD5.Create())
            {
                var oldHash = md5.ComputeHash(File.OpenRead("Models/test.jpg"));
                var newHash = md5.ComputeHash(File.OpenRead("Models/result-direct.jpg"));

                Assert.IsTrue(oldHash.SequenceEqual(newHash));
            }
        }

        [TestMethod]
        public Task trying_to_download_when_no_chunks_present()
        {
            new DB("mongodb-entities-test-multi");

            Assert.ThrowsException<InvalidOperationException>(
                () =>
                {
                    using (var stream = File.OpenWrite("test.file"))
                    {
                        DB.File<Image>(ObjectId.GenerateNewId().ToString())
                                .DownloadAsync(stream).GetAwaiter().GetResult();
                    }
                });

            return Task.CompletedTask;
        }
    }
}
