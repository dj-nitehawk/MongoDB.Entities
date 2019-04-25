using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities;
using MongoDB.Driver.Linq;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class Saving
    {
        [TestMethod]
        public void saving_new_document_returns_an_id()
        {
            var book = new Book { Title = "Test" };
            book.Save();
            var idEmpty = string.IsNullOrEmpty(book.ID);
            Assert.IsFalse(idEmpty);
            book.Delete();
        }

        [TestMethod]
        public void saved_book_has_correct_title()
        {
            var book = new Book { Title = "Test" };
            book.Save();
            var title = book.Collection().Where(b => b.ID == book.ID).Select(b => b.Title).SingleOrDefault();
            Assert.AreEqual("Test", title);
            book.Delete();
        }

        [TestMethod]
        public void embedding_non_entity_returns_correct_document()
        {
            var book = new Book { Title = "Test" };
            book.Review = new Review { Stars = 5, Reviewer = "New York Times" };
            book.Save();
            var res = book.Collection()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.Review.Reviewer)
                          .SingleOrDefault();
            Assert.AreEqual("New York Times", res);
            book.Delete();
        }

        [TestMethod]
        public void embedding_with_ToDocument_returns_correct_document()
        {
            var book = new Book { Title = "Test" };
            var author = new Author { Name = "Test Author" };
            book.RelatedAuthor = author.ToDocument();
            book.Save();
            var res = book.Collection()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.RelatedAuthor.Name)
                          .SingleOrDefault();
            Assert.AreEqual(res, book.RelatedAuthor.Name);
            book.Delete();
        }

        [TestMethod]
        public void embedding_with_ToDocument_returns_blank_id()
        {
            var book = new Book { Title = "Test" };
            var author = new Author { Name = "Test Author" };
            book.RelatedAuthor = author.ToDocument();
            book.Save();
            var res = book.Collection()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.RelatedAuthor.ID)
                          .SingleOrDefault();
            Assert.AreEqual(res, "000000000000000000000000");
            book.Delete();
        }

        [TestMethod]
        public void embedding_with_ToDocuments_returns_correct_documents()
        {
            var book = new Book { Title = "Test" }; book.Save();
            var author1 = new Author { Name = "Eckhart Tolle" }; author1.Save();
            var author2 = new Author { Name = "Nisargadatta Maharaj" }; author2.Save();
            book.OtherAuthors = (new Author[] { author1, author2 }).ToDocuments();
            //todo: unit tests
        }

        //[TestMethod]
        //public void test_name()
        //{

        //}
    }
}
