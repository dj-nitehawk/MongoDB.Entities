using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
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
            new DB("mongodb-entities-test-multi");

            var img = new Image { Height = 800, Width = 600, Name = "Test.Png" };
            await img.SaveAsync();

            using var stream = File.Open("Models/test.png", FileMode.Open);
            await img.UploadDataAsync(stream, 1024);

        }
    }
}
