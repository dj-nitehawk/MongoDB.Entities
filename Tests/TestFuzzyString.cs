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
        public async System.Threading.Tasks.Task fuzzystring_type_saving_and_retrieval_worksAsync()
        {
            var guid = Guid.NewGuid().ToString();

            await new Book { Title = "fstsarw", Review = new Review { Alias = guid } }.SaveAsync();

            var res = await DB.Queryable<Book>()
                        .Where(b => b.Review.Alias.Value == guid)
                        .SingleAsync();

            Assert.AreEqual(guid, res.Review.Alias.Value);
        }
    }
}
