using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var book = new Book { Title = "book" };
            var author = new Author { Name = "sotorrce" };
            author.Save();
            book.MainAuthor = author.ToReference();
            book.Save();
            var res = book.Collection()
                          .Where(b => b.ID == book.ID)
                          .Single()
                          .MainAuthor.ToEntity();
            Assert.AreEqual(author.Name, res.Name);
        }

        [TestMethod]
        public void adding_one_to_many_references_returns_correct_entities()
        {
            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "aotmrrceb1" };
            var book2 = new Book { Title = "aotmrrceb2" };
            book1.Save(); book2.Save();
            author.Books.Add(book1);
            author.Books.Add(book2);
            //todo: query and test
        }

        [TestMethod]
        public void removing_one_to_many_references_removes_correct_entities()
        {

        }

    }
}
