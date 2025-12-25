using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public class PagedSearchEntity
{
    [TestMethod]
    public async Task empty_results()
    {
        var guid = Guid.NewGuid().ToString();

        var (results, _, pageCount) = await DB.Default
                                              .PagedSearch<BookEntity>()
                                              .Match(b => b.ID == guid)
                                              .Sort(b => b.ID, Order.Ascending)
                                              .PageNumber(1)
                                              .PageSize(200)
                                              .ExecuteAsync();

        Assert.AreEqual(0, pageCount);
        Assert.IsEmpty(results);
    }

    static Task SeedData(string guid)
    {
        var list = new List<BookEntity>(10);

        for (var i = 1; i <= 10; i++)
            list.Add(new() { Title = guid });

        return DB.Default.SaveAsync(list);
    }

    [TestMethod]
    public async Task got_results()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var (results, _, pageCount) = await DB.Default
                                              .PagedSearch<BookEntity>()
                                              .Match(b => b.Title == guid)
                                              .Sort(b => b.ID, Order.Ascending)
                                              .PageNumber(2)
                                              .PageSize(5)
                                              .ExecuteAsync();

        Assert.AreEqual(2, pageCount);
        Assert.IsNotEmpty(results);
    }

    [TestMethod]
    public async Task correctly_rounded_page_count()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var (results, _, pageCount) = await DB.Default
                                              .PagedSearch<BookEntity>()
                                              .Match(b => b.Title == guid)
                                              .Sort(b => b.ID, Order.Ascending)
                                              .PageNumber(1)
                                              .PageSize(3)
                                              .ExecuteAsync();

        Assert.AreEqual(4, pageCount);
        Assert.IsNotEmpty(results);
    }

    [TestMethod]
    public async Task got_results_with_fluent()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var db = DB.Default;

        var pipeline = db.Fluent<BookEntity>()
                         .Match(b => b.Title == guid);

        var (results, _, pageCount) = await db
                                            .PagedSearch<BookEntity>()
                                            .WithFluent(pipeline)
                                            .Sort(b => b.ID, Order.Ascending)
                                            .PageNumber(2)
                                            .PageSize(5)
                                            .ExecuteAsync();

        Assert.AreEqual(2, pageCount);
        Assert.IsNotEmpty(results);
    }

    class BookResult
    {
        public string BookTitle { get; set; }
        public string BookID { get; set; }
    }

    [TestMethod]
    public async Task with_projection()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        _ = await DB.Default
                    .PagedSearch<BookEntity, BookResult>()
                    .Match(b => b.Title == guid)
                    .Sort(b => b.ID, Order.Ascending)
                    .Project(b => new() { BookID = b.ID.ToString(), BookTitle = b.Title })
                    .PageNumber(1)
                    .PageSize(5)
                    .ExecuteAsync();
    }

    [TestMethod]
    public async Task sort_by_meta_text_score_with_projection()
    {
        var db = DB.Default;

        await db.DropCollectionAsync<GenreEntity>();

        await db.Index<GenreEntity>()
                .Key(g => g.Name, KeyType.Text)
                .Option(o => o.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid();

        var list = new[]
        {
            new GenreEntity { GuidID = guid, Position = 0, Name = "this should not match" },
            new GenreEntity { GuidID = guid, Position = 3, Name = "one two three four five six" },
            new GenreEntity { GuidID = guid, Position = 4, Name = "one two three four five six seven" },
            new GenreEntity { GuidID = guid, Position = 2, Name = "one two three four five six seven eight" },
            new GenreEntity { GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine" }
        };

        await db.SaveAsync(list);

        var (results, _, _) = await db
                                    .PagedSearch<GenreEntity>()
                                    .Match(Search.Full, "one eight nine")
                                    .Project(p => new() { Name = p.Name, Position = p.Position })
                                    .SortByTextScore()
                                    .ExecuteAsync();

        Assert.HasCount(4, results);
        Assert.AreEqual(1, results[0].Position);
        Assert.AreEqual(4, results[^1].Position);
    }

    [TestMethod]
    public async Task sort_by_meta_text_score_no_projection()
    {
        var db = DB.Default;

        await db.DropCollectionAsync<GenreEntity>();

        await db.Index<GenreEntity>()
                .Key(g => g.Name, KeyType.Text)
                .Option(o => o.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid();

        var list = new[]
        {
            new GenreEntity { GuidID = guid, Position = 0, Name = "this should not match" },
            new GenreEntity { GuidID = guid, Position = 3, Name = "one two three four five six" },
            new GenreEntity { GuidID = guid, Position = 4, Name = "one two three four five six seven" },
            new GenreEntity { GuidID = guid, Position = 2, Name = "one two three four five six seven eight" },
            new GenreEntity { GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine" }
        };

        await db.SaveAsync(list);

        var (results, _, _) = await db
                                    .PagedSearch<GenreEntity>()
                                    .Match(Search.Full, "one eight nine")
                                    .SortByTextScore()
                                    .ExecuteAsync();

        Assert.HasCount(4, results);
        Assert.AreEqual(1, results[0].Position);
        Assert.AreEqual(4, results[^1].Position);
    }

    [TestMethod]
    public async Task exclusion_projection_works()
    {
        var author = new AuthorEntity
        {
            Name = "name",
            Surname = "sername",
            Age = 22,
            FullName = "fullname"
        };
        await DB.Default.SaveAsync(author);

        var (res, _, _) = await DB.Default.PagedSearch<AuthorEntity>()
                                  .Match(a => a.ID == author.ID)
                                  .Sort(a => a.ID, Order.Ascending)
                                  .ProjectExcluding(a => new { a.Age, a.Name })
                                  .ExecuteAsync();

        Assert.AreEqual(author.FullName, res[0].FullName);
        Assert.AreEqual(author.Surname, res[0].Surname);
        Assert.AreEqual(0, res[0].Age);
        Assert.IsNull(res[0].Name);
    }
}