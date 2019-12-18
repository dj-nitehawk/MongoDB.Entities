using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Dates
    {
        [TestMethod]
        public void not_setting_date_doesnt_cause_issues()
        {
            var book = new Book { Title = "nsddci" };
            book.Save();

            var res = DB.Find<Book>().One(book.ID);

            Assert.AreEqual(res.Title, book.Title);
            Assert.IsNull(res.PublishedOn);
        }

        [TestMethod]
        public void date_props_contain_correct_value()
        {
            var pubDate = DateTime.UtcNow;

            var book = new Book
            {
                Title = "dpccv",
                PublishedOn = pubDate
            };
            book.Save();

            var res = DB.Find<Book>().One(book.ID);

            Assert.AreEqual(pubDate.Ticks, res.PublishedOn.Ticks);
            Assert.AreEqual(pubDate.ToUniversalTime(), res.PublishedOn.DateTime);
            Assert.AreEqual(pubDate, res.PublishedOn.DateTime);
            Assert.AreEqual(DateTimeKind.Utc, res.PublishedOn.DateTime.Kind);
        }

        [TestMethod]
        public void querying_on_ticks_work()
        {
            var pubDate = DateTime.UtcNow;

            var book = new Book
            {
                Title = "qotw",
                PublishedOn = pubDate
            };
            book.Save();

            var res = DB.Find<Book>()
                        .Match(b => b.ID == book.ID && b.PublishedOn.Ticks == pubDate.Ticks)
                        .Execute()
                        .Single();

            Assert.AreEqual(book.ID, res.ID);

            res = DB.Find<Book>()
                    .Match(b => b.ID == book.ID && b.PublishedOn.Ticks < pubDate.Ticks + TimeSpan.FromSeconds(1).Ticks)
                    .Execute()
                    .Single();

            Assert.AreEqual(book.ID, res.ID);
        }

        [TestMethod]
        public void querying_on_datetime_prop_works()
        {
            var pubDate = DateTime.UtcNow;

            var book = new Book
            {
                Title = "qodtpw",
                PublishedOn = pubDate
            };
            book.Save();

            var res = DB.Find<Book>()
            .Match(b => b.ID == book.ID && b.PublishedOn.DateTime == pubDate)
            .Execute()
            .Single();

            Assert.AreEqual(book.ID, res.ID);

            res = DB.Find<Book>()
                    .Match(b => b.ID == book.ID && b.PublishedOn.DateTime < pubDate.AddSeconds(1))
                    .Execute()
                    .Single();

            Assert.AreEqual(book.ID, res.ID);
        }

        [TestMethod]
        public void setting_ticks_creates_correct_datetime()
        {
            var now = DateTime.UtcNow;

            var date = new Date() { Ticks = now.Ticks };

            Assert.AreEqual(now, date.DateTime);
        }
    }
}
