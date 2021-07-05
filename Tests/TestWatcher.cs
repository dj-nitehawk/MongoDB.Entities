using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

            await Task.Delay(300);

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

            await Task.Delay(300);

            Assert.AreEqual(4, allFlowers.Count);
        }

        [TestMethod]
        public async Task watching_with_projection_works()
        {
            var watcher = DB.Watcher<Flower>("test-with-projection");
            var allFlowers = new List<Flower>();

            watcher.Start(
                EventType.Created | EventType.Updated,
                f => new Flower { Color = f.Color },
                f => f.FullDocument.Color == "red");

            await Task.Delay(300);

            watcher.OnChanges +=
                flowers => allFlowers.AddRange(flowers);

            await new[] {
                new Flower { Name = "test", Color = "red" },
                new Flower { Name = "test", Color = "red" },
                new Flower { Name = "test", Color = "red" }
            }.SaveAsync();

            var flower = new Flower { Name = "test" };
            await flower.SaveAsync();

            await flower.DeleteAsync();

            await Task.Delay(300);

            Assert.AreEqual(3, allFlowers.Count);
            Assert.IsTrue(allFlowers[0].Name == null && allFlowers[0].Color == "red");
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

            await Task.Delay(300);

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

            await Task.Delay(300);

            Assert.AreEqual(4, allFlowers.Count);
        }

        [TestMethod]
        public async Task watching_with_filter_builders_CSD()
        {
            var guid = Guid.NewGuid().ToString();

            var watcher = DB.Watcher<Flower>("test-with-filter-builders-csd");
            var allFlowers = new List<Flower>();

            watcher.Start(
                EventType.Created | EventType.Updated,
                b => b.Eq(d => d.FullDocument.Name, guid));

            await Task.Delay(300);

            watcher.OnChangesCSD +=
                csDocs => allFlowers.AddRange(csDocs.Select(x => x.FullDocument));

            await new[] {
                new Flower { Name = guid },
                new Flower { Name = "exclude me" },
                new Flower { Name = guid },
                new Flower { Name = guid },
            }.SaveAsync();

            var flower = new Flower { Name = guid };
            await flower.SaveAsync();

            await flower.DeleteAsync();

            await Task.Delay(300);

            Assert.AreEqual(4, allFlowers.Count);
        }
    }
}
