using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests.Bugs
{
    [TestClass]
    public class _58_MultiDB_broken
    {
        private class DummyEntity : Entity
        {
        }

        [TestMethod]
        public async Task Write_in_first_db_and_count()
        {
            var db = new DB("first");
            try
            {
                await db.SaveAsync(new DummyEntity());
                var items = await db.Find<DummyEntity>().Match(g => true).ExecuteAsync();
                Assert.AreEqual(1, items.Count);
            }
            finally
            {
                var client = new MongoClient();
                client.DropDatabase("first");
            }
        }

        [TestMethod]
        public async Task Write_in_second_db_and_count()
        {
            var db = new DB("second");
            try
            {
                await db.SaveAsync(new DummyEntity());
                var items = await db.Find<DummyEntity>().Match(g => true).ExecuteAsync();
                Assert.AreEqual(1, items.Count);
            }
            finally
            {
                var client = new MongoClient();
                client.DropDatabase("second");
            }
        }
    }
}
