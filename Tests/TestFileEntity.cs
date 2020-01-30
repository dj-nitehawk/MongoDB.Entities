using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using MongoDB.Entities.Utilities;
using System.IO;
using System.Linq;
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

            //using var stream = await new HttpClient().GetStreamAsync("https://djnitehawk.com/test/test.bmp");

            using var stream = File.Open("Models/test.png", FileMode.Open);
            await img.UploadDataAsync(stream);

            var count = db.Queryable<FileChunk>()
                          .Where(c => c.FileID == img.ID)
                          .Count();

            Assert.AreEqual(2318430, img.FileSize);
            Assert.AreEqual(img.ChunkCount, count);
        }
    }
}
