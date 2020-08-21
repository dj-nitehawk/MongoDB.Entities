using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class MultiDb
    {
        private static readonly DB db = null;

        static MultiDb()
        {
            db = new DB("mongodb-entities-test-multi");
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

        [TestMethod]
        public void get_instance_by_db_name()
        {
            new DB("test1");
            new DB("test2");

            var res = DB.GetInstance("test2");

            Assert.AreEqual("test2", res.DatabaseName());
        }

        [TestMethod]
        public void uninitialized_get_instance_throws()
        {
            Assert.ThrowsException<InvalidOperationException>(() => DB.GetInstance("some-database"));
        }

        [TestMethod]
        public void multiple_initializations_should_not_throw()
        {
            new DB("multi-init");
            new DB("multi-init");

            var db = new DB("multi-init");
            var instance = DB.GetInstance("multi-init");

            Assert.AreEqual("multi-init", instance.DatabaseName());
            Assert.AreEqual("multi-init", db.DatabaseName());
        }

        [TestMethod]
        public void dropping_collections()
        {
            var guid = Guid.NewGuid().ToString();
            var marks = new[] {
                new BookMark{ BookName = guid},
                new BookMark{ BookName = guid},
                new BookMark{ BookName = guid},
            };
            marks.Save();

            var covers = new[] {
                new BookCover{  BookID = guid },
                new BookCover{  BookID = guid },
                new BookCover{  BookID = guid }
            };

            covers.Save();

            foreach (var cover in covers)
            {
                cover.BookMarks += marks;
            }

            Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == marks.Length));

            db.DropCollection<BookMark>();

            Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == 0));

            Assert.AreEqual(3, db.Queryable<BookCover>().Where(b => b.BookID == guid).Count());
        }

        [TestMethod]
        public async Task async_dropping_collections()
        {
            var guid = Guid.NewGuid().ToString();
            var marks = new[] {
                new BookMark{ BookName = guid},
                new BookMark{ BookName = guid},
                new BookMark{ BookName = guid},
            };
            marks.Save();

            var covers = new[] {
                new BookCover{  BookID = guid },
                new BookCover{  BookID = guid },
                new BookCover{  BookID = guid }
            };

            covers.Save();

            foreach (var cover in covers)
            {
                cover.BookMarks += marks;
            }

            Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == marks.Length));

            await db.DropCollectionAsync<BookMark>().ConfigureAwait(false);

            Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == 0));

            Assert.AreEqual(3, db.Queryable<BookCover>().Where(b => b.BookID == guid).Count());
        }
    }
}
