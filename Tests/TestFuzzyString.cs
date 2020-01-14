using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class FuzzyStringTest
    {
        [TestMethod]
        public void fuzzystring_type_saving_and_retrieval_works()
        {
            var guid = Guid.NewGuid().ToString();

            (new Book { Title = "fstsarw", Review = new Review { Alias = guid } }).Save();

            var res = DB.Queryable<Book>()
                        .Where(b => b.Review.Alias.Value == guid)
                        .Single();

            Assert.AreEqual(guid, res.Review.Alias.Value);
        }
    }
}
