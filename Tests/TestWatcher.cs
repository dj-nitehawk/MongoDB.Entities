using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Watcher
    {
        [TestMethod]
        public void watching_works()
        {
            var cancellation = new CancellationTokenSource();
            var watcher = DB.Watch<Flower>(EventType.Created | EventType.Deleted, 5, 1, cancellation.Token);
            var allFlowers = new List<Flower>();
            var aborted = false;

            Task.Delay(1000).Wait();

            watcher.OnEvents +=
                flowers => allFlowers.AddRange(flowers);

            watcher.OnAbort +=
                () => aborted = true;

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

            cancellation.Cancel();

            Task.Delay(1000).Wait();

            Assert.IsTrue(aborted);
        }
    }
}
