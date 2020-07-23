using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    //[TestClass]
    public class Watcher
    {
        private static CancellationTokenSource cancellation = new CancellationTokenSource();
        private static Watcher<Flower> watcher;
        private static readonly List<Flower> allFlowers = new List<Flower>();
        private static bool aborted = false;

        [ClassInitialize]
        public static void init(TestContext _)
        {
            watcher = DB.Watch<Flower>(EventType.Created | EventType.Deleted, 5, 1, cancellation.Token);

            watcher.OnEvents +=
                flowers => allFlowers.AddRange(flowers);

            watcher.OnAbort +=
                () => aborted = true;
        }

        [TestMethod]
        public void watching_works()
        {
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
