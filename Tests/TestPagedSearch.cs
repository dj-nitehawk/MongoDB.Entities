using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class PagedSearch
    {
        [TestMethod]
        public async Task empty_results()
        {
            var guid = Guid.NewGuid().ToString();

            var (Results, PageCount) = await DB
                .PagedSearch<Book>()
                .Match(b => b.ID == guid)
                .Sort(b => b.ID, Order.Ascending)
                .PageNumber(1)
                .PageSize(200)
                .ExecuteAsync();

            Assert.AreEqual(0, PageCount);
            Assert.IsTrue(Results.Count == 0);
        }

        private static Task SeedData(string guid)
        {
            var list = new List<Book>(10);

            for (int i = 1; i <= 10; i++)
            {
                list.Add(new Book { Title = guid });
            }

            return list.SaveAsync();
        }

        [TestMethod]
        public async Task got_results()
        {
            var guid = Guid.NewGuid().ToString();

            await SeedData(guid);

            var (Results, PageCount) = await DB
                .PagedSearch<Book>()
                .Match(b => b.Title == guid)
                .Sort(b => b.ID, Order.Ascending)
                .PageNumber(2)
                .PageSize(5)
                .ExecuteAsync();

            Assert.AreEqual(2, PageCount);
            Assert.IsTrue(Results.Count > 0);
        }

        [TestMethod]
        public async Task with_projection()
        {
            var guid = Guid.NewGuid().ToString();

            await SeedData(guid);

            var (Results, PageCount) = await DB
                .PagedSearch<Book, string>()
                .Match(b => b.Title == guid)
                .Sort(b => b.ID, Order.Ascending)
                .Project(b=> )
                .PageNumber(2)
                .PageSize(5)
                .ExecuteAsync();
        }
    }
}
