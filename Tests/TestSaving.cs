using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task async_saved_book_has_correct_title()
        {
            var book = new Book { Title = "Test" };
            await book.SaveAsync().ConfigureAwait(false);
            var title = book.Queryable().Where(b => b.ID == book.ID).Select(b => b.Title).SingleOrDefault();
            Assert.AreEqual("Test", title);
        }

        [TestMethod]
        public void created_on_property_works()
        {
            var author = new Author { Name = "test" };
            author.Save();

            var res = DB.Find<Author, DateTime>()
                        .Match(a => a.ID == author.ID)
                        .Project(a => a.CreatedOn)
                        .Execute()
                        .Single();

            Assert.AreEqual(res.ToLongTimeString(), author.CreatedOn.ToLongTimeString());
            Assert.IsTrue(DateTime.UtcNow.Subtract(res).TotalSeconds <= 5);
        }

        [TestMethod]
        public void save_preserving()
        {
            var book = new Book { Title = "Title is preserved", Price = 123.45m, DontSaveThis = 111 };
            book.Save();

            book.Title = "updated title";
            book.Price = 543.21m;

            book.SavePreserving(b => new
            {
                b.Title,
                b.PublishedOn,
                b.Review.Stars,
                Something = b.ReviewArray
            });

            book = DB.Find<Book>().One(book.ID);

            Assert.AreEqual("Title is preserved", book.Title);
            Assert.AreEqual(543.21m, book.Price);
            Assert.AreEqual(default, book.DontSaveThis);
        }

        [TestMethod]
        public async Task async_save_preserving()
        {
            var book = new Book { Title = "Title is preserved", Price = 123.45m, DontSaveThis = 111 };
            book.Save();

            book.Title = "updated title";
            book.Price = 543.21m;

            await book.SavePreservingAsync(b => new
            {
                b.Title,
                b.PublishedOn,
                b.Review.Stars,
                Something = b.ReviewArray
            }).ConfigureAwait(false);

            book = DB.Find<Book>().One(book.ID);

            Assert.AreEqual("Title is preserved", book.Title);
            Assert.AreEqual(543.21m, book.Price);
            Assert.AreEqual(default, book.DontSaveThis);
        }

        [TestMethod]
        public void save_preserving_inverse_attribute()
        {
            var book = new Book
            {
                Title = "original", //dontpreserve
                Price = 100, //dontpreserve
                PriceDbl = 666,
                MainAuthor = ObjectId.GenerateNewId().ToString()
            };
            book.Save();

            book.Title = "updated";
            book.Price = 111;
            book.PriceDbl = 999;
            book.MainAuthor = null;

            book.SavePreserving();

            var res = DB.Find<Book>().One(book.ID);

            Assert.AreEqual(res.Title, book.Title);
            Assert.AreEqual(res.Price, book.Price);
            Assert.AreEqual(res.PriceDbl, 666);
            Assert.IsFalse(res.MainAuthor.ID == null);
        }

        [TestMethod]
        public void save_preserving_attribute()
        {
            var author = new Author
            {
                Age = 123,
                Name = "initial name",
                FullName = "initial fullname",
                Birthday = DateTime.UtcNow
            };
            author.Save();

            author.Name = "updated author name";
            author.Age = 666; //preserve
            author.Age2 = 400; //preserve
            author.Birthday = DateTime.MinValue; //preserve
            author.FullName = null;
            author.BestSeller = ObjectId.GenerateNewId().ToString();

            author.SavePreserving();

            var res = DB.Find<Author>().One(author.ID);

            Assert.AreEqual("updated author name", res.Name);
            Assert.AreEqual(123, res.Age);
            Assert.AreEqual(default, res.Age2);
            Assert.AreNotEqual(DateTime.MinValue, res.Birthday);
            Assert.AreEqual("initial fullname", res.FullName);
            Assert.AreEqual(author.BestSeller.ID, res.BestSeller.ID);
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
            Assert.AreEqual(book.RelatedAuthor.ID, res);
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
            Assert.AreEqual(book.OtherAuthors[0].ID, authors[0].ID);
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
            Assert.AreEqual(book.OtherAuthors[0].ID, authors[0].ID);
        }

        [TestMethod]
        public void find_by_lambda_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid }; author1.Save();
            var author2 = new Author { Name = guid }; author2.Save();

            var res = DB.Find<Author>().Many(a => a.Name == guid);

            Assert.AreEqual(2, res.Count);
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
        public async Task async_find_by_id_returns_correct_document()
        {
            var book1 = new Book { Title = "fbircdb1" }; book1.Save();
            var book2 = new Book { Title = "fbircdb2" }; book2.Save();

            var res1 = await DB.Find<Book>().OneAsync(new ObjectId().ToString()).ConfigureAwait(false);
            var res2 = await DB.Find<Book>().OneAsync(book2.ID).ConfigureAwait(false);

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
        public async Task async_find_by_filter_basic_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid }; author1.Save();
            var author2 = new Author { Name = guid }; author2.Save();

            var res = await DB.Find<Author>().ManyAsync(f => f.Eq(a => a.Name, guid)).ConfigureAwait(false);

            Assert.AreEqual(2, res.Count());
        }

        [TestMethod]
        public void find_by_multiple_match_methods()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = DB.Find<Author>()
                        .Match(a => a.Age > 10)
                        .Match(a => a.Surname == guid)
                        .Execute();

            Assert.AreEqual(3, res.Count);
            Assert.IsFalse(res.Any(a => a.Age == 10));
        }

        [TestMethod]
        public async Task async_find_by_multiple_match_methods()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = await DB.Find<Author>()
                        .Match(a => a.Age > 10)
                        .Match(a => a.Surname == guid)
                        .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(3, res.Count);
            Assert.IsFalse(res.Any(a => a.Age == 10));
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

        [TestMethod]
        public async Task async_find_by_filter_returns_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = await DB.Find<Author>()
                        .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                        .Sort(a => a.Age, Order.Descending)
                        .Sort(a => a.Name, Order.Descending)
                        .Skip(1)
                        .Limit(1)
                        .Project(p => p.Include("Name").Include("Surname"))
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                        .ExecuteAsync().ConfigureAwait(false);

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
        public async Task async_find_with_projection_to_custom_type_works()
        {
            var guid = Guid.NewGuid().ToString();
            var one = new Author { Name = "a", Age = 10, Surname = guid }; one.Save();
            var two = new Author { Name = "b", Age = 20, Surname = guid }; two.Save();
            var three = new Author { Name = "c", Age = 30, Surname = guid }; three.Save();
            var four = new Author { Name = "d", Age = 40, Surname = guid }; four.Save();

            var res = (await DB.Find<Author, Test>()
                        .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                        .Sort(a => a.Age, Order.Descending)
                        .Sort(a => a.Name, Order.Descending)
                        .Skip(1)
                        .Limit(1)
                        .Project(a => new Test { Tester = a.Name })
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                        .ExecuteAsync().ConfigureAwait(false))
                        .FirstOrDefault();

            Assert.AreEqual(three.Name, res.Tester);

        }

        [TestMethod]
        public void find_with_exclusion_projection_works()
        {
            var author = new Author
            {
                Name = "name",
                Surname = "sername",
                Age = 22,
                FullName = "fullname"
            };
            author.Save();

            var res = DB.Find<Author>()
                        .Match(a => a.ID == author.ID)
                        .ProjectExcluding(a => new { a.Age, a.Name })
                        .Execute()
                        .Single();

            Assert.AreEqual(author.FullName, res.FullName);
            Assert.AreEqual(author.Surname, res.Surname);
            Assert.IsTrue(res.Age == default);
            Assert.IsTrue(res.Name == default);
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

            var res = DB.Find<Author>()
                        .MatchExpression("{$and:[{$gt:['$Age2','$Age']},{$eq:['$Surname','" + guid + "']}]}")
                        .Execute()
                        .Single();

            Assert.AreEqual(res.Surname, guid);
        }

        [TestMethod]
        public void find_with_aggregation_expression_using_template_works()
        {
            var guid = Guid.NewGuid().ToString();
            var author = new Author { Name = "a", Age = 10, Age2 = 11, Surname = guid }; author.Save();

            var template = new Template<Author>("{$and:[{$gt:['$<Age2>','$<Age>']},{$eq:['$<Surname>','<guid>']}]}")
                    .Path(a => a.Age2)
                    .Path(a => a.Age)
                    .Path(a => a.Surname)
                    .Tag("guid", guid);

            var res = DB.Find<Author>()
                        .MatchExpression(template)
                        .Execute()
                        .Single();

            Assert.AreEqual(res.Surname, guid);
        }

        [TestMethod]
        public void find_fluent_with_aggregation_expression_works()
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
            var book1 = new Book { Title = guid, Price = 100.123m }; book1.Save();
            var book2 = new Book { Title = guid, Price = 100.123m }; book2.Save();

            var res = DB.Queryable<Book>()
                        .Where(b => b.Title == guid)
                        .GroupBy(b => b.Title)
                        .Select(g => new
                        {
                            Title = g.Key,
                            Sum = g.Sum(b => b.Price)
                        }).Single();

            Assert.AreEqual(book1.Price + book2.Price, res.Sum);
        }

        [TestMethod]
        public void ignore_if_defaults_convention_works()
        {
            var author = new Author
            {
                Name = "test"
            };
            author.Save();

            var res = DB.Find<Author>().One(author.ID);

            Assert.IsTrue(res.Age == 0);
            Assert.IsTrue(res.Birthday == null);
        }
    }
}
