using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Watcher
    {
        [TestMethod]
        public void watching_works()
        {
            var watcher = DB.Watcher<Flower>("test");
            var allFlowers = new List<Flower>();

            watcher.Start(EventType.Created | EventType.Deleted, 5);

            Task.Delay(1000).Wait();

            watcher.OnChanges +=
                flowers => allFlowers.AddRange(flowers);

            new[] {
                new Flower { Name = "test" },
                new Flower { Name = "test" },
                new Flower { Name = "test" }
            }.Save();

            var flower = new Flower { Name = "test" };
            flower.Save();
            flower.Delete();

            Task.Delay(1000).Wait();

            Assert.AreEqual(5, allFlowers.Count);
        }
    }
}
