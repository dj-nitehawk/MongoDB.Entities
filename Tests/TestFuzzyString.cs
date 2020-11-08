using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class FuzzyStringTest
    {
        [TestMethod]
        public async Task fuzzystring_type_saving_and_retrieval_worksAsync()
        {
            var guid = Guid.NewGuid().ToString();

            await new Book { Title = "fstsarw", Review = new Review { Alias = guid } }.SaveAsync();

            var res = await DB.Queryable<Book>()
                        .Where(b => b.Review.Alias.Value == guid)
                        .SingleAsync();

            Assert.AreEqual(guid, res.Review.Alias.Value);
        }

        [TestMethod]
        public async Task fuzzystring_type_with_nulls_workAsync()
        {
            var guid = Guid.NewGuid().ToString();

            await new Book { Title = guid, Review = new Review { Alias = null } }.SaveAsync();

            var res = await DB.Queryable<Book>()
                        .Where(b => b.Title == guid)
                        .SingleAsync();

            Assert.AreEqual(null, res.Review.Alias.Value);
        }
    }
}
