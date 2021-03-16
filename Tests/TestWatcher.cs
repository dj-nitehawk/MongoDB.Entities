using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Watcher
    {
        [TestMethod]
        public async Task watching_works()
        {
            var watcher = DB.Watcher<Flower>("test");
            var allFlowers = new List<Flower>();

            watcher.Start(
                EventType.Created | EventType.Updated,
                f => f.FullDocument.Name == "test");

            Task.Delay(1000).Wait();

            watcher.OnChanges +=
                flowers => allFlowers.AddRange(flowers);

            await new[] {
                new Flower { Name = "test" },
                new Flower { Name = "test" },
                new Flower { Name = "test" }
            }.SaveAsync();

            var flower = new Flower { Name = "test" };
            await flower.SaveAsync();

            await flower.DeleteAsync();

            Task.Delay(1000).Wait();

            Assert.AreEqual(4, allFlowers.Count);
        }

        [TestMethod]
        public async Task watching_with_filter_builders()
        {
            var guid = Guid.NewGuid().ToString();

            var watcher = DB.Watcher<Flower>("test-with-filter-builders");
            var allFlowers = new List<Flower>();

            watcher.Start(
                EventType.Created | EventType.Updated,
                b => b.Eq(d => d.FullDocument.Name, guid));

            Task.Delay(1000).Wait();

            watcher.OnChanges +=
                flowers => allFlowers.AddRange(flowers);

            await new[] {
                new Flower { Name = guid },
                new Flower { Name = guid },
                new Flower { Name = guid }
            }.SaveAsync();

            var flower = new Flower { Name = guid };
            await flower.SaveAsync();

            await flower.DeleteAsync();

            Task.Delay(1000).Wait();

            Assert.AreEqual(4, allFlowers.Count);
        }
    }
}
