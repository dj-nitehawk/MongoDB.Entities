using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class SavingEntity
{
    [TestMethod]
    public async Task saving_new_document_returns_an_id()
    {
        var book = new BookEntity { Title = "Test" };
        await DB.Default.SaveAsync(book);
        var idEmpty = string.IsNullOrEmpty(book.ID);
        Assert.IsFalse(idEmpty);
    }

    [TestMethod]
    public async Task saved_book_has_correct_title()
    {
        var db = DB.Default;
        var book = new BookEntity { Title = "Test" };
        await db.SaveAsync(book);
        var title = db.Queryable<BookEntity>().Where(b => b.ID == book.ID).Select(b => b.Title).SingleOrDefault();
        Assert.AreEqual("Test", title);
    }

    [TestMethod]
    public async Task insert_single_entity()
    {
        var db = DB.Default;

        var author = new AuthorEntity { Name = "test" };
        await db.InsertAsync(author);

        var res = await db.Find<AuthorEntity>().MatchID(author.ID).ExecuteAnyAsync();

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task insert_batch()
    {
        var guid = Guid.NewGuid().ToString();

        var db = DB.Default;

        var author1 = new AuthorEntity { Name = guid };
        var author2 = new AuthorEntity { Name = guid };

        await db.InsertAsync([author1, author2]);

        var res = await db.Find<AuthorEntity>()
                          .Match(a => a.Name == guid)
                          .ExecuteAsync();

        Assert.HasCount(2, res);
    }

    [TestMethod]
    public async Task created_on_property_works()
    {
        var db = DB.Default;

        var author = new AuthorEntity { Name = "test" };
        await db.SaveAsync(author);

        var res = (await db.Find<AuthorEntity, DateTime>()
                           .Match(a => a.ID == author.ID)
                           .Project(a => a.CreatedOn)
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(res.ToLongTimeString(), author.CreatedOn.ToLongTimeString());
        Assert.IsLessThanOrEqualTo(5, DateTime.UtcNow.Subtract(res).TotalSeconds);
    }

    [TestMethod]
    public async Task save_partially_single_include()
    {
        var db = DB.Default;
        var book = new BookEntity { Title = "test book", Price = 100 };

        await db.SaveOnlyAsync(book, b => new { b.Title });

        var res = await db.Find<BookEntity>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(0, res!.Price);
        Assert.AreEqual("test book", res.Title);

        res.Price = 200;

        await db.SaveOnlyAsync(res, b => new { b.Price });

        res = await db.Find<BookEntity>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual(200, res!.Price);
    }

    [TestMethod]
    public async Task save_partially_single_include_string()
    {
        var db = DB.Default;
        var book = new BookEntity { Title = "test book", Price = 100 };

        await db.SaveOnlyAsync(book, new List<string> { "Title" });

        var res = await db.Find<BookEntity>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(0, res!.Price);
        Assert.AreEqual("test book", res.Title);

        res.Price = 200;

        await db.SaveOnlyAsync(res, new List<string> { "Price" });

        res = await db.Find<BookEntity>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual(200, res!.Price);
    }

    [TestMethod]
    public async Task save_partially_batch_include()
    {
        var books = new[]
        {
            new BookEntity { Title = "one", Price = 100 },
            new BookEntity { Title = "two", Price = 200 }
        };

        var db = DB.Default;
        await db.SaveOnlyAsync(books, b => new { b.Title });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await db.Find<BookEntity>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(0, res[0].Price);
        Assert.AreEqual(0, res[1].Price);
        Assert.AreEqual("one", res[0].Title);
        Assert.AreEqual("two", res[1].Title);
    }

    [TestMethod]
    public async Task save_partially_batch_include_string()
    {
        var books = new[]
        {
            new BookEntity { Title = "one", Price = 100 },
            new BookEntity { Title = "two", Price = 200 }
        };

        var db = DB.Default;
        await db.SaveOnlyAsync(books, new List<string> { "Title" });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await db.Find<BookEntity>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(0, res[0].Price);
        Assert.AreEqual(0, res[1].Price);
        Assert.AreEqual("one", res[0].Title);
        Assert.AreEqual("two", res[1].Title);
    }

    [TestMethod]
    public async Task save_partially_single_exclude()
    {
        var book = new BookEntity { Title = "test book", Price = 100 };

        var db = DB.Default;
        await db.SaveExceptAsync(book, b => new { b.Title });

        var res = await db.Find<BookEntity>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(100, res!.Price);
        Assert.IsNull(res.Title);

        res.Title = "updated";

        await db.SaveExceptAsync(res, b => new { b.Price });

        res = await db.Find<BookEntity>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual("updated", res!.Title);
    }

    [TestMethod]
    public async Task save_partially_single_exclude_string()
    {
        var book = new BookEntity { Title = "test book", Price = 100 };

        var db = DB.Default;
        await db.SaveExceptAsync(book, new List<string> { "Title" });

        var res = await db.Find<BookEntity>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(100, res!.Price);
        Assert.IsNull(res.Title);

        res.Title = "updated";

        await db.SaveExceptAsync(res, new List<string> { "Price" });

        res = await db.Find<BookEntity>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual("updated", res!.Title);
    }

    [TestMethod]
    public async Task save_partially_batch_exclude()
    {
        var books = new[]
        {
            new BookEntity { Title = "one", Price = 100 },
            new BookEntity { Title = "two", Price = 200 }
        };
        var db = DB.Default;
        await db.SaveExceptAsync(books, b => new { b.Title });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await DB.Default.Find<BookEntity>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(100, res[0].Price);
        Assert.AreEqual(200, res[1].Price);
        Assert.IsNull(res[0].Title);
        Assert.IsNull(res[1].Title);
    }

    [TestMethod]
    public async Task save_partially_batch_exclude_string()
    {
        var books = new[]
        {
            new BookEntity { Title = "one", Price = 100 },
            new BookEntity { Title = "two", Price = 200 }
        };
        var db = DB.Default;
        await db.SaveExceptAsync(books, new List<string> { "Title" });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await DB.Default.Find<BookEntity>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(100, res[0].Price);
        Assert.AreEqual(200, res[1].Price);
        Assert.IsNull(res[0].Title);
        Assert.IsNull(res[1].Title);
    }

    [TestMethod]
    public async Task save_preserving_upsert()
    {
        var book = new BookEntity { Title = "Original Title", Price = 123.45m, DontSaveThis = 111 };

        book.ID = (string)book.GenerateNewID();
        book.Title = "updated title";
        book.Price = 543.21m;
        var db = DB.Default;
        await db.SavePreservingAsync(book);

        book = await DB.Default.Find<BookEntity>().OneAsync(book.ID);

        Assert.AreEqual("updated title", book!.Title);
        Assert.AreEqual(543.21m, book.Price);
        Assert.AreEqual(0, book.DontSaveThis);
    }

    [TestMethod]
    public async Task save_preserving()
    {
        var book = new BookEntity { Title = "Original Title", Price = 123.45m, DontSaveThis = 111 };
        var db = DB.Default;
        await db.SaveAsync(book);

        book.Title = "updated title";
        book.Price = 543.21m;

        await db.SavePreservingAsync(book);

        book = await db.Find<BookEntity>().OneAsync(book.ID);

        Assert.AreEqual("updated title", book!.Title);
        Assert.AreEqual(543.21m, book.Price);
        Assert.AreEqual(0, book.DontSaveThis);
    }

    [TestMethod]
    public async Task save_preserving_inverse_attribute()
    {
        var book = new BookEntity
        {
            Title = "original", //dontpreserve
            Price = 100,        //dontpreserve
            PriceDbl = 666,
            MainAuthor = new(ObjectId.GenerateNewId().ToString()!)
        };
        var db = DB.Default;
        await db.SaveAsync(book);

        book.Title = "updated";
        book.Price = 111;
        book.PriceDbl = 999;
        book.MainAuthor = null!;

        await db.SavePreservingAsync(book);

        var res = await db.Find<BookEntity>().OneAsync(book.ID);

        Assert.AreEqual(res!.Title, book.Title);
        Assert.AreEqual(res.Price, book.Price);
        Assert.AreEqual(666, res.PriceDbl);
        Assert.IsNotNull(res.MainAuthor.ID);
    }

    [TestMethod]
    public async Task save_preserving_attribute()
    {
        var author = new AuthorEntity
        {
            Age = 123,
            Name = "initial name",
            FullName = "initial fullname",
            Birthday = DateTime.UtcNow.ToDate()
        };
        var db = DB.Default;
        await db.SaveAsync(author);

        author.Name = "updated author name";
        author.Age = 666;                         //preserve
        author.Age2 = 400;                        //preserve
        author.Birthday = new(DateTime.MinValue); //preserve
        author.FullName = null;
        author.BestSeller = new(ObjectId.GenerateNewId().ToString()!);

        await db.SavePreservingAsync(author);

        var res = await db.Find<AuthorEntity>().OneAsync(author.ID);

        Assert.AreEqual("updated author name", res!.Name);
        Assert.AreEqual(123, res.Age);
        Assert.AreEqual(0, res.Age2);
        Assert.AreNotEqual(DateTime.MinValue, res.Birthday.DateTime);
        Assert.AreEqual("initial fullname", res.FullName);
        Assert.AreEqual(author.BestSeller.ID, res.BestSeller.ID);
    }

    [TestMethod]
    public async Task embedding_non_entity_returns_correct_document()
    {
        var book = new BookEntity
        {
            Title = "Test",
            Review = new() { Stars = 5, Reviewer = "enercd" }
        };
        var db = DB.Default;
        await db.SaveAsync(book);
        var res = db.Queryable<BookEntity>()
                    .Where(b => b.ID == book.ID)
                    .Select(b => b.Review.Reviewer)
                    .SingleOrDefault();
        Assert.AreEqual(book.Review.Reviewer, res);
    }

    [TestMethod]
    public async Task embedding_with_ToDocument_returns_correct_doc()
    {
        var book = new BookEntity { Title = "Test" };
        var author = new AuthorEntity { Name = "ewtdrcd" };
        book.RelatedAuthor = author.ToDocument();
        var db = DB.Default;
        await db.SaveAsync(book);
        var res = db.Queryable<BookEntity>()
                    .Where(b => b.ID == book.ID)
                    .Select(b => b.RelatedAuthor.Name)
                    .SingleOrDefault();
        Assert.AreEqual(book.RelatedAuthor.Name, res);
    }

    [TestMethod]
    public async Task embedding_with_ToDocument_returns_blank_id()
    {
        var book = new BookEntity { Title = "Test" };
        var author = new AuthorEntity { Name = "Test Author" };
        book.RelatedAuthor = author.ToDocument();
        var db = DB.Default;
        await db.SaveAsync(book);
        var res = db.Queryable<BookEntity>()
                    .Where(b => b.ID == book.ID)
                    .Select(b => b.RelatedAuthor.ID)
                    .SingleOrDefault();
        Assert.AreEqual(book.RelatedAuthor.ID, res);
    }

    [TestMethod]
    public async Task embedding_with_ToDocuments_Arr_returns_correct_docs()
    {
        var db = DB.Default;
        var book = new BookEntity { Title = "Test" };
        await db.SaveAsync(book);
        var author1 = new AuthorEntity { Name = "ewtrcd1" };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "ewtrcd2" };
        await db.SaveAsync(author2);
        book.OtherAuthors = new[] { author1, author2 }.ToDocuments();
        await db.SaveAsync(book);
        var authors = db.Queryable<BookEntity>()
                        .Where(b => b.ID == book.ID)
                        .Select(b => b.OtherAuthors).Single();
        Assert.HasCount(2, authors);
        Assert.AreEqual(author2.Name, authors[1].Name);
        Assert.AreEqual(book.OtherAuthors[0].ID, authors[0].ID);
    }

    [TestMethod]
    public async Task embedding_with_ToDocuments_IEnumerable_returns_correct_docs()
    {
        var db = DB.Default;
        var book = new BookEntity { Title = "Test" };
        await db.SaveAsync(book);
        var author1 = new AuthorEntity { Name = "ewtrcd1" };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "ewtrcd2" };
        await db.SaveAsync(author2);
        var list = new List<AuthorEntity> { author1, author2 };
        book.OtherAuthors = list.ToDocuments().ToArray();
        await db.SaveAsync(book);
        var authors = db.Queryable<BookEntity>()
                        .Where(b => b.ID == book.ID)
                        .Select(b => b.OtherAuthors).Single();
        Assert.HasCount(2, authors);
        Assert.AreEqual(author2.Name, authors[1].Name);
        Assert.AreEqual(book.OtherAuthors[0].ID, authors[0].ID);
    }

    [TestMethod]
    public async Task find_with_ignore_global_filter()
    {
        var db = new MyDbEntity();

        var guid = Guid.NewGuid().ToString();

        await db.SaveAsync(
        [
            new() { Name = guid, Age = 200 },
            new() { Name = guid, Age = 200 },
            new AuthorEntity { Name = guid, Age = 111 }
        ]);

        var res = await db.Find<AuthorEntity>()
                          .Match(a => a.Name == guid)
                          .IgnoreGlobalFilters()
                          .ExecuteAsync();

        Assert.HasCount(3, res);
    }

    [TestMethod]
    public async Task queryable_with_global_filter()
    {
        var db = new MyDbEntity();

        var guid = Guid.NewGuid().ToString();

        await db.SaveAsync(
        [
            new() { Name = guid, Age = 200 },
            new() { Name = guid, Age = 200 },
            new AuthorEntity { Name = guid, Age = 111 }
        ]);

        var res = await db.Queryable<AuthorEntity>()
                          .Where(a => a.Name == guid)
                          .ToListAsync();

        Assert.HasCount(1, res);
    }

    [TestMethod]
    public async Task global_filter_for_base_class()
    {
        var guid = Guid.NewGuid().ToString();

        var db = new MyBaseEntityDb();

        var flowers = new[]
        {
            new FlowerEntity { Name = guid, CreatedBy = "xyz" },
            new FlowerEntity { Name = guid },
            new FlowerEntity { Name = guid }
        };

        await db.SaveAsync(flowers);

        var res = await db.Find<FlowerEntity>().Match(f => f.Name == guid).ExecuteAsync();

        Assert.HasCount(1, res);
    }

    [TestMethod]
    public async Task global_filter_for_interface_prepend()
    {
        var db = new MyDbFlower(prepend: true);

        var guid = Guid.NewGuid().ToString();

        var flowers = new[]
        {
            new FlowerEntity { Name = guid, IsDeleted = true },
            new FlowerEntity { Name = guid },
            new FlowerEntity { Name = guid }
        };

        await db.SaveAsync(flowers);

        var res = await db.Find<FlowerEntity>().Match(f => f.Name == guid).ExecuteAsync();

        Assert.HasCount(2, res);
    }

    [TestMethod]
    public async Task find_by_lambda_returns_correct_documents()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author2);

        var res = await db.Find<AuthorEntity>().ManyAsync(a => a.Name == guid);

        Assert.HasCount(2, res);
    }

    [TestMethod]
    public async Task find_by_id_returns_correct_document()
    {
        var db = new MyDbEntity();
        var book1 = new BookEntity { Title = "fbircdb1" };
        await db.SaveAsync(book1);
        var book2 = new BookEntity { Title = "fbircdb2" };
        await db.SaveAsync(book2);

        var res1 = await db.Find<BookEntity>().OneAsync(ObjectId.GenerateNewId().ToString()!);
        var res2 = await db.Find<BookEntity>().OneAsync(book2.ID);

        Assert.IsNull(res1);
        Assert.AreEqual(book2.ID, res2!.ID);
    }

    [TestMethod]
    public async Task find_by_filter_basic_returns_correct_documents()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author2);

        var res = await db.Find<AuthorEntity>().ManyAsync(f => f.Eq(a => a.Name, guid));

        Assert.HasCount(2, res);
    }

    [TestMethod]
    public async Task find_by_filter_single()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author2);

        var res = await db.Find<AuthorEntity>()
                          .Match(f => f.Eq(a => a.Name, guid))
                          .ExecuteFirstAsync();

        Assert.AreEqual(author1.ID, res!.ID);
    }

    [TestMethod]
    public async Task find_by_filter_any()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = guid };
        await db.SaveAsync(author2);

        var res = await db.Find<AuthorEntity>()
                          .Match(f => f.Eq(a => a.Name, guid))
                          .ExecuteAnyAsync();

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task find_by_multiple_match_methods()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorEntity { Name = "a", Age = 10, Surname = guid };
        await db.SaveAsync(one);
        var two = new AuthorEntity { Name = "b", Age = 20, Surname = guid };
        await db.SaveAsync(two);
        var three = new AuthorEntity { Name = "c", Age = 30, Surname = guid };
        await db.SaveAsync(three);
        var four = new AuthorEntity { Name = "d", Age = 40, Surname = guid };
        await db.SaveAsync(four);

        var res = await db.Find<AuthorEntity>()
                          .Match(a => a.Age > 10)
                          .Match(a => a.Surname == guid)
                          .ExecuteAsync();

        Assert.HasCount(3, res);
        Assert.IsFalse(res.Any(a => a.Age == 10));
    }

    [TestMethod]
    public async Task find_by_filter_returns_correct_documents()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorEntity { Name = "a", Age = 10, Surname = guid };
        await db.SaveAsync(one);
        var two = new AuthorEntity { Name = "b", Age = 20, Surname = guid };
        await db.SaveAsync(two);
        var three = new AuthorEntity { Name = "c", Age = 30, Surname = guid };
        await db.SaveAsync(three);
        var four = new AuthorEntity { Name = "d", Age = 40, Surname = guid };
        await db.SaveAsync(four);

        var res = await db.Find<AuthorEntity>()
                          .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                          .Sort(a => a.Age, Order.Descending)
                          .Sort(a => a.Name, Order.Descending)
                          .Skip(1)
                          .Limit(1)
                          .Project(p => p.Include("Name").Include("Surname"))
                          .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                          .ExecuteAsync();

        Assert.AreEqual(three.Name, res[0].Name);
    }

    class Test
    {
        public string Tester { get; init; }
    }

    [TestMethod]
    public async Task find_with_projection_to_custom_type_works()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorEntity { Name = "a", Age = 10, Surname = guid };
        await db.SaveAsync(one);
        var two = new AuthorEntity { Name = "b", Age = 20, Surname = guid };
        await db.SaveAsync(two);
        var three = new AuthorEntity { Name = "c", Age = 30, Surname = guid };
        await db.SaveAsync(three);
        var four = new AuthorEntity { Name = "d", Age = 40, Surname = guid };
        await db.SaveAsync(four);

        var res = (await db.Find<AuthorEntity, Test>()
                           .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                           .Sort(a => a.Age, Order.Descending)
                           .Sort(a => a.Name, Order.Descending)
                           .Skip(1)
                           .Limit(1)
                           .Project(a => new() { Tester = a.Name })
                           .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                           .ExecuteAsync())
            .FirstOrDefault();

        Assert.AreEqual(three.Name, res!.Tester);
    }

    [TestMethod]
    public async Task find_with_exclusion_projection_works()
    {
        var author = new AuthorEntity
        {
            Name = "name",
            Surname = "sername",
            Age = 22,
            FullName = "fullname"
        };
        var db = DB.Default;
        await db.SaveAsync(author);

        var res = (await db.Find<AuthorEntity>()
                           .Match(a => a.ID == author.ID)
                           .ProjectExcluding(a => new { a.Age, a.Name })
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(author.FullName, res.FullName);
        Assert.AreEqual(author.Surname, res.Surname);
        Assert.AreEqual(0, res.Age);
        Assert.IsNull(res.Name);
    }

    [TestMethod]
    public async Task find_with_aggregation_pipeline_returns_correct_docs()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorEntity { Name = "a", Age = 10, Surname = guid };
        await db.SaveAsync(one);
        var two = new AuthorEntity { Name = "b", Age = 20, Surname = guid };
        await db.SaveAsync(two);
        var three = new AuthorEntity { Name = "c", Age = 30, Surname = guid };
        await db.SaveAsync(three);
        var four = new AuthorEntity { Name = "d", Age = 40, Surname = guid };
        await db.SaveAsync(four);

        var res = await db.Fluent<AuthorEntity>()
                          .Match(a => a.Surname == guid && a.Age > 10)
                          .SortByDescending(a => a.Age)
                          .ThenByDescending(a => a.Name)
                          .Skip(1)
                          .Limit(1)
                          .Project(a => new { Test = a.Name })
                          .FirstOrDefaultAsync();

        Assert.AreEqual(three.Name, res.Test);
    }

    [TestMethod]
    public async Task find_with_aggregation_expression_works()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author = new AuthorEntity { Name = "a", Age = 10, Age2 = 11, Surname = guid };
        await db.SaveAsync(author);

        var res = (await db.Find<AuthorEntity>()
                           .MatchExpression("{$and:[{$gt:['$Age2','$Age']},{$eq:['$Surname','" + guid + "']}]}")
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(res.Surname, guid);
    }

    [TestMethod]
    public async Task find_with_aggregation_expression_using_template_works()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author = new AuthorEntity { Name = "a", Age = 10, Age2 = 11, Surname = guid };
        await db.SaveAsync(author);

        var template = new Template<AuthorEntity>("{$and:[{$gt:['$<Age2>','$<Age>']},{$eq:['$<Surname>','<guid>']}]}")
                       .Path(a => a.Age2)
                       .Path(a => a.Age)
                       .Path(a => a.Surname)
                       .Tag("guid", guid);

        var res = (await db.Find<AuthorEntity>()
                           .MatchExpression(template)
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(res.Surname, guid);
    }

    [TestMethod]
    public async Task find_fluent_with_aggregation_expression_works()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author = new AuthorEntity { Name = "a", Age = 10, Age2 = 11, Surname = guid };
        await db.SaveAsync(author);

        var res = await db.Fluent<AuthorEntity>()
                          .Match(a => a.Surname == guid)
                          .MatchExpression("{$gt:['$Age2','$Age']}")
                          .SingleAsync();

        Assert.AreEqual(res.Surname, guid);
    }

    [TestMethod]
    public async Task find_with_include_required_props()
    {
        var review = new ReviewEntity
        {
            Stars = 5,         //req
            Reviewer = "test", //req
            Rating = 1
        };
        var db = DB.Default;
        await db.SaveAsync(review);

        var res = await db.Find<ReviewEntity>()
                          .MatchID(review.Id)
                          .Project(r => new() { Rating = r.Rating })
                          .IncludeRequiredProps()
                          .ExecuteSingleAsync();

        Assert.AreEqual(5, res!.Stars);
        Assert.AreEqual("test", res.Reviewer);
        Assert.AreEqual(1, res.Rating);
    }

    [TestMethod]
    public async Task update_and_get_with_include_required_props()
    {
        var review = new ReviewEntity
        {
            Stars = 5,         //req
            Reviewer = "test", //req
            Rating = 1
        };
        var db = DB.Default;
        await db.SaveAsync(review);

        var res = await db.UpdateAndGet<ReviewEntity>()
                          .MatchID(review.Id)
                          .Modify(r => r.Rating, 10)
                          .Project(r => new() { Rating = r.Rating })
                          .IncludeRequiredProps()
                          .ExecuteAsync();

        Assert.AreEqual(5, res!.Stars);
        Assert.AreEqual("test", res.Reviewer);
        Assert.AreEqual(10, res.Rating);
    }

    [TestMethod]
    public async Task decimal_properties_work_correctly()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var book1 = new BookEntity { Title = guid, Price = 100.123m };
        await db.SaveAsync(book1);
        var book2 = new BookEntity { Title = guid, Price = 100.123m };
        await db.SaveAsync(book2);

        var res = db.Queryable<BookEntity>()
                    .Where(b => b.Title == guid)
                    .GroupBy(b => b.Title)
                    .Select(
                        g => new
                        {
                            Title = g.Key,
                            Sum = g.Sum(b => b.Price)
                        }).Single();

        Assert.AreEqual(book1.Price + book2.Price, res.Sum);
    }

    [TestMethod]
    public async Task ignore_if_defaults_convention_works()
    {
        var author = new AuthorEntity
        {
            Name = "test"
        };
        var db = DB.Default;
        await db.SaveAsync(author);

        var res = await db.Find<AuthorEntity>().OneAsync(author.ID);

        Assert.AreEqual(0, res!.Age);
        Assert.IsNull(res.Birthday);
    }

    [TestMethod]
    public async Task custom_id_generation_logic_works()
    {
        var db = DB.Default;
        var customer = new CustomerWithCustomID();
        await db.SaveAsync(customer);

        var res = await db.Find<CustomerWithCustomID>().OneAsync(customer.ID);

        Assert.AreEqual(res!.ID, customer.ID);
    }

    [TestMethod]
    public async Task custom_id_used_in_a_relationship()
    {
        var db = DB.Default;
        var customer = new CustomerWithCustomID();
        await db.SaveAsync(customer);

        var book = new BookEntity
        {
            Title = "ciuiar",
            Customer = customer.ToReference()
        };
        await db.SaveAsync(book);

        var res = await book.Customer.ToEntityAsync(db);
        Assert.AreEqual(res.ID, customer.ID);

        var cus = await db.Queryable<BookEntity>()
                          .Where(b => b.Customer.ID == customer.ID)
                          .Select(b => b.Customer)
                          .SingleOrDefaultAsync();
        Assert.AreEqual(cus.ID, customer.ID);
    }

    [TestMethod]
    public async Task custom_id_override_string()
    {
        var e = new CustomIDOverride();
        await DB.Default.SaveAsync(e);
        await Task.Delay(100);

        var creationTime = new DateTime(long.Parse(e.ID));

        Assert.IsTrue(creationTime < DateTime.UtcNow);
    }

    [TestMethod]
    public async Task custom_id_override_objectid()
    {
        var x = new CustomIDOverride
        {
            ID = ObjectId.GenerateNewId().ToString()!
        };
        await DB.Default.SaveAsync(x);

        Assert.IsTrue(ObjectId.TryParse(x.ID, out _));
    }

    [TestMethod]
    public async Task custom_id_duplicate_throws()
    {
        var one = new CustomIDDuplicate();
        var two = new CustomIDDuplicate();
        await Assert.ThrowsExactlyAsync<MongoBulkWriteException<CustomIDDuplicate>>(async () => await DB.Default.SaveAsync([one, two]));
    }
}