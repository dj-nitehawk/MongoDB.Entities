using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Counting
    {
        private Task Init(string guid)
        {
            var list = new List<Author>();

            for (int i = 1; i <= 25; i++)
            {
                list.Add(new Author { Name = guid });
            }

            return list.SaveAsync();
        }

        [TestMethod]
        public async Task count_estimated_works()
        {
            var guid = Guid.NewGuid().ToString();
            await Init(guid);

            var count = await DB.CountEstimatedAsync<Author>();

            Assert.IsTrue(count > 0);
        }

        [TestMethod]
        public async Task count_with_lambda()
        {
            var guid = Guid.NewGuid().ToString();
            await Init(guid);

            var count = await DB.CountAsync<Author>(a => a.Name == guid);

            Assert.AreEqual(25, count);
        }

        [TestMethod]
        public async Task count_with_filter_definition()
        {
            var guid = Guid.NewGuid().ToString();
            await Init(guid);

            var filter = DB.Filter<Author>()
                            .Eq(a => a.Name, guid);

            var count = await DB.CountAsync(filter);

            Assert.AreEqual(25, count);
        }

        [TestMethod]
        public async Task count_with_filter_builder()
        {
            var guid = Guid.NewGuid().ToString();
            await Init(guid);

            var count = await DB.CountAsync<Author>(b => b.Eq(a => a.Name, guid));

            Assert.AreEqual(25, count);
        }
    }
}
