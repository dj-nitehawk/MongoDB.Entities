using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
            var title = book.Queryable().Where(b => b.ID == book.ID).Select(b => b.Title).SingleOrDefault();
            Assert.AreEqual("Test", title);
        }

        [TestMethod]
        public void embedding_non_entity_returns_correct_document()
        {
            var book = new Book { Title = "Test" };
            book.Review = new Review { Stars = 5, Reviewer = "enercd" };
            book.Save();
            var res = book.Queryable()
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
            var res = book.Queryable()
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
            var res = book.Queryable()
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
            var authors = book.Queryable()
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
            var authors = book.Queryable()
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

            var res = DB.Find<Author>().Many(a => a.Name == guid);

            Assert.AreEqual(2, res.Count());
        }

        [TestMethod]
        public void find_by_id_returns_correct_document()
        {
            var book1 = new Book { Title = "fbircdb1" }; book1.Save();
            var book2 = new Book { Title = "fbircdb2" }; book2.Save();

            var res1 = DB.Find<Book>().One(new ObjectId().ToString());
            var res2 = DB.Find<Book>().One(book2.ID);

            Assert.AreEqual(null, res1);
            Assert.AreEqual(book2.ID, res2.ID);
        }

        [TestMethod]
        public void find_by_filter_basic_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid }; author1.Save();
            var author2 = new Author { Name = guid }; author2.Save();

            var res = DB.Find<Author>().Many(f => f.Eq(a => a.Name, guid));

            Assert.AreEqual(2, res.Count());
        }

        [TestMethod]
        public void find_by_filter_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = DB.Find<Author>()
                        .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                        .Sort(a => a.Age, Order.Descending)
                        .Sort(a => a.Name, Order.Descending)
                        .Skip(1)
                        .Limit(1)
                        .Project(p => p.Include("Name").Include("Surname"))
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                        .Execute();

            Assert.AreEqual(three.Name, res.First().Name);
        }

        private class Test { public string Tester { get; set; } }
        [TestMethod]
        public void find_with_projection_to_custom_type_works()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = DB.Find<Author, Test>()
                        .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                        .Sort(a => a.Age, Order.Descending)
                        .Sort(a => a.Name, Order.Descending)
                        .Skip(1)
                        .Limit(1)
                        .Project(a => new Test { Tester = a.Name })
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                        .Execute()
                        .FirstOrDefault();

            Assert.AreEqual(three.Name, res.Tester);

        }

        [TestMethod]
        public void find_with_aggregation_pipeline_returns_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = DB.Fluent<Author>()
                        .Match(a => a.Surname == guid && a.Age > 10)
                        .SortByDescending(a => a.Age)
                        .ThenByDescending(a => a.Name)
                        .Skip(1)
                        .Limit(1)
                        .Project(a => new { Test = a.Name })
                        .FirstOrDefault();

            Assert.AreEqual(three.Name, res.Test);
        }

        [TestMethod]
        public void find_with_aggregation_expression_works()
        {
            var guid = Guid.NewGuid().ToString();
            var author = new Author { Name = "a", Age = 10, Age2 = 11, Surname = guid }; author.Save();

            var res = DB.Fluent<Author>()
                        .Match(a => a.Surname == guid)
                        .MatchExpression("{$gt:['$Age2','$Age']}")
                        .Single();

            Assert.AreEqual(res.Surname, guid);
        }

        [TestMethod]
        public void decimal_properties_work_correctly()
        {
            var guid = Guid.NewGuid().ToString();
            var book1 = new Book { Title = guid, SellingPrice = 100.123m }; book1.Save();
            var book2 = new Book { Title = guid, SellingPrice = 100.123m }; book2.Save();

            var res = DB.Queryable<Book>()
                        .Where(b => b.Title == guid)
                        .GroupBy(b => b.Title)
                        .Select(g => new
                        {
                            Title = g.Key,
                            Sum = g.Sum(b => b.SellingPrice)
                        }).Single();

            Assert.AreEqual(book1.SellingPrice + book2.SellingPrice, res.Sum);
        }

        [TestMethod]
        public void nested_prop_full_path_test()
        {
            Expression<Func<Book, object>> exp = x => x.MoreReviews[-1].Rating;
            var res = exp.FullPath();
            Assert.AreEqual("MoreReviews.Rating", res);

            Expression<Func<Book, object>> exp1 = x => x.MoreReviews[-1].Books[-1].MoreReviews[-1].Books[-1].ModifiedOn;
            var res1 = exp1.FullPath();
            Assert.AreEqual("MoreReviews.Books.MoreReviews.Books.ModifiedOn", res1);
        }
    }
}
