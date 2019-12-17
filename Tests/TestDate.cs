using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

        }
    }
}
