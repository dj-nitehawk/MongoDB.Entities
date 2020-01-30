using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using System.Diagnostics;
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

            //using var stream = await new System.Net.Http.HttpClient().GetStreamAsync("https://djnitehawk.com/test/test.bmp");
            //await img.UploadDataWithTimeoutAsync(stream, 7, 4096);

            using var stream = File.OpenRead("Models/test.jpg");
            await img.UploadDataAsync(stream);

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
            await img.UploadDataAsync(stream);

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
                await img.UploadDataAsync(inStream);
            }

            using (var outStream = File.OpenWrite("Models/result.jpg"))
            {
                await img.DownloadDataAsync(outStream, 3);
            }

            using (var md5 = MD5.Create())
            {
                var oldHash = md5.ComputeHash(File.OpenRead("Models/test.jpg"));
                var newHash = md5.ComputeHash(File.OpenRead("Models/result.jpg"));

                Assert.IsTrue(oldHash.SequenceEqual(newHash));
            }
        }
    }
}
