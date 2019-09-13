using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class MultiDb
    {
        private static DB db = null;

        static MultiDb()
        {
            db = new DB("mongodb-entitites-test-multi");
        }

        [TestMethod]
        public void save_entity_works()
        {
            var cover = new BookCover
            {
                BookID = "123",
                BookName = "test book " + Guid.NewGuid().ToString()
            };

            cover.Save();
            Assert.IsNotNull(cover.ID);

            var res = db.Find<BookCover>().One(cover.ID);

            Assert.AreEqual(cover.ID, res.ID);
            Assert.AreEqual(cover.BookName, res.BookName);
        }

        [TestMethod]
        public void relationships_work()
        {
            var cover = new BookCover
            {
                BookID = "123",
                BookName = "test book " + Guid.NewGuid().ToString()
            };
            cover.Save();

            var mark = new BookMark
            {
                BookCover = cover.ToReference(),
                BookName = cover.BookName,
            };

            mark.Save();

            cover.BookMarks.Add(mark);

            var res = cover.BookMarks.ChildrenQueryable().First();

            Assert.AreEqual(cover.BookName, res.BookName);

            Assert.AreEqual(res.BookCover.ToEntity().ID, cover.ID);
        }
    }

}
