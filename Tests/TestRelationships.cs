using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities;
using MongoDB.Driver.Linq;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Relationships
    {
        [TestMethod]
        public void setting_one_to_one_reference_returns_correct_entity()
        {
            var book = new Book { Title = "sotorrce" };
            var author = new Author { Name = "author" };
            author.Save();
            book.MainAuthor = author.ToReference();
            book.Save();
            var res = book.Collection()
                          .Where()
        }

    }
}
