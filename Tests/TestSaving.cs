using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities.Tests
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
        }

        [TestMethod]
        public void saved_book_has_correct_title()
        {
            var book = new Book { Title = "Test" };
            book.Save();
            var title = book.Collection().Where(b => b.ID == book.ID).Select(b => b.Title).SingleOrDefault();
            Assert.AreEqual("Test", title);
        }

        [TestMethod]
        public void embedding_non_entity_returns_correct_document()
        {
            var book = new Book { Title = "Test" };
            book.Review = new Review { Stars = 5, Reviewer = "enercd" };
            book.Save();
            var res = book.Collection()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.Review.Reviewer)
                          .SingleOrDefault();
            Assert.AreEqual(book.Review.Reviewer, res);
        }

        [TestMethod]
        public void embedding_with_ToDocument_returns_correct_doc()
        {
            var book = new Book { Title = "Test" };
            var author = new Author { Name = "ewtdrcd" };
            book.RelatedAuthor = author.ToDocument();
            book.Save();
            var res = book.Collection()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.RelatedAuthor.Name)
                          .SingleOrDefault();
            Assert.AreEqual(book.RelatedAuthor.Name, res);
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
            Assert.AreEqual("000000000000000000000000", res);
        }

        [TestMethod]
        public void embedding_with_ToDocuments_Arr_returns_correct_docs()
        {
            var book = new Book { Title = "Test" }; book.Save();
            var author1 = new Author { Name = "ewtrcd1" }; author1.Save();
            var author2 = new Author { Name = "ewtrcd2" }; author2.Save();
            book.OtherAuthors = (new Author[] { author1, author2 }).ToDocuments();
            book.Save();
            var authors = book.Collection()
                              .Where(b => b.ID == book.ID)
                              .Select(b => b.OtherAuthors).Single();
            Assert.AreEqual(authors.Count(), 2);
            Assert.AreEqual(author2.Name, authors[1].Name);
            Assert.AreEqual("000000000000000000000000", authors[0].ID);
        }

        [TestMethod]
        public void embedding_with_ToDocuments_IEnumerable_returns_correct_docs()
        {
            var book = new Book { Title = "Test" }; book.Save();
            var author1 = new Author { Name = "ewtrcd1" }; author1.Save();
            var author2 = new Author { Name = "ewtrcd2" }; author2.Save();
            var list = new List<Author>() { author1, author2 };
            book.OtherAuthors = list.ToDocuments().ToArray();
            book.Save();
            var authors = book.Collection()
                              .Where(b => b.ID == book.ID)
                              .Select(b => b.OtherAuthors).Single();
            Assert.AreEqual(authors.Count(), 2);
            Assert.AreEqual(author2.Name, authors[1].Name);
            Assert.AreEqual("000000000000000000000000", authors[0].ID);
        }

        [TestMethod]
        public void find_by_lambda_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid }; author1.Save();
            var author2 = new Author { Name = guid }; author2.Save();
            var res = DB.Find<Author>(a => a.Name == guid);
            Assert.AreEqual(2, res.Count());
        }

        [TestMethod]
        public void find_by_id_returns_correct_document()
        {
            var book1 = new Book { Title = "fbircdb1" }; book1.Save();
            var book2 = new Book { Title = "fbircdb2" }; book2.Save();
            Assert.AreEqual(null, DB.Find<Book>(new ObjectId().ToString()));
            Assert.AreEqual(book2.ID, DB.Find<Book>(book2.ID).ID);
        }

        [TestMethod]
        public void find_by_filter_returns_correct_documents()
        {

        }

        [TestMethod]
        public void decimal_properties_work_correctly()
        {
            var guid = Guid.NewGuid().ToString();
            var book1 = new Book { Title = guid, SellingPrice = 100.123m }; book1.Save();
            var book2 = new Book { Title = guid, SellingPrice = 100.123m }; book2.Save();

            var res = DB.Collection<Book>()
                        .Where(b => b.Title == guid)
                        .GroupBy(b => b.Title)
                        .Select(g => new {
                            Title = g.Key,
                            Sum = g.Sum(b=>b.SellingPrice)
                        }).Single();

            Assert.AreEqual(book1.SellingPrice + book2.SellingPrice, res.Sum);
        }
    }
}
