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
        public void adding_one2many_references_returns_correct_entities()
        {
            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "aotmrrceb1" };
            var book2 = new Book { Title = "aotmrrceb2" };
            book1.Save(); book2.Save();
            author.Save();
            author.Books.Add(book1);
            author.Books.Add(book2);
            var books = author.Collection()
                              .Where(a => a.ID == author.ID)
                              .Single()
                              .Books
                              .Collection().ToArray();
            Assert.AreEqual(book2.Title, books[1].Title);
        }

        [TestMethod]
        public void removing_a_one2many_ref_removes_correct_entities()
        {
            var book = new Book { Title = "rotmrrceb" };
            book.Save();
            var author1 = new Author { Name = "rotmrrcea1" }; author1.Save();
            var author2 = new Author { Name = "rotmrrcea2" }; author2.Save();
            book.GoodAuthors.Add(author1);
            book.GoodAuthors.Add(author2);
            var remAuthor = DB.Collection<Author>()
                              .Where(a => a.ID == author2.ID)
                              .Single();
            book.GoodAuthors.Remove(remAuthor);
            var resBook = book.Collection()
                              .Where(b => b.ID == book.ID)
                              .Single();
            Assert.AreEqual(1, resBook.GoodAuthors.Collection().Count());
            Assert.AreEqual(author1.Name, resBook.GoodAuthors.Collection().First().Name);
        }

        [TestMethod]
        public void collection_shortcut_of_many_returns_correct_children()
        {
            var book = new Book { Title = "book" };
            book.Save();
            var author1 = new Author { Name = "csomrcc1" }; author1.Save();
            var author2 = new Author { Name = "csomrcc1" }; author2.Save();
            book.GoodAuthors.Add(author1);
            book.GoodAuthors.Add(author2);
            Assert.AreEqual(2, book.GoodAuthors.Collection().Count());
            Assert.AreEqual(author1.Name, book.GoodAuthors.Collection().First().Name);
        }

        [TestMethod]
        public void test_more_than_1_one2many_properties_on_entity()
        {
            var book1 = new Book { Title = "tmt1o2mpoeb1" }; book1.Save();
            var book2 = new Book { Title = "tmt1o2mpoeb1" }; book1.Save();

            var author1 = new Author { Name = "tmt1o2mpoea1" }; author1.Save();
            var author2 = new Author { Name = "tmt1o2mpoea1" }; author2.Save();

            book1.GoodAuthors.Add(author1);
            book1.BadAuthors.Add(author1);
            //todo: test
        }
    }
}
