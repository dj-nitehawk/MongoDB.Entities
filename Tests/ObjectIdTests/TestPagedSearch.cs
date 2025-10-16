using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Entities.Tests;

[TestClass]
public class PagedSearchObjectId
{
    [TestMethod]
    public async Task empty_results()
    {
        var oId = ObjectId.Empty;

        var (Results, _, PageCount) =  await DB.Instance()
                                            .PagedSearch<BookObjectId>()
                                            .Match(b => b.ID == oId)
                                            .Sort(b => b.ID, Order.Ascending)
                                            .PageNumber(1)
                                            .PageSize(200)
                                            .ExecuteAsync();

        Assert.AreEqual(0, PageCount);
        Assert.IsTrue(Results.Count == 0);
    }

    static Task SeedData(string ObjectId)
    {
        var list = new List<BookObjectId>(10);

        for (var i = 1; i <= 10; i++)
            list.Add(new() { Title = ObjectId });

        return list.SaveAsync();
    }

    [TestMethod]
    public async Task got_results()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var (Results, _, PageCount) =  await DB.Instance()
                                            .PagedSearch<BookObjectId>()
                                            .Match(b => b.Title == guid)
                                            .Sort(b => b.ID, Order.Ascending)
                                            .PageNumber(2)
                                            .PageSize(5)
                                            .ExecuteAsync();

        Assert.AreEqual(2, PageCount);
        Assert.IsTrue(Results.Count > 0);
    }

    [TestMethod]
    public async Task correctly_rounded_page_count()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var (Results, _, PageCount) =  await DB.Instance()
                                            .PagedSearch<BookObjectId>()
                                            .Match(b => b.Title == guid)
                                            .Sort(b => b.ID, Order.Ascending)
                                            .PageNumber(1)
                                            .PageSize(3)
                                            .ExecuteAsync();

        Assert.AreEqual(4, PageCount);
        Assert.IsTrue(Results.Count > 0);
    }

    [TestMethod]
    public async Task got_results_with_fluent()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var pipeline = DB.Instance().Fluent<BookObjectId>()
                         .Match(b => b.Title == guid);

        var (Results, _, PageCount) =  await DB.Instance()
                                            .PagedSearch<BookObjectId>()
                                            .WithFluent(pipeline)
                                            .Sort(b => b.ID, Order.Ascending)
                                            .PageNumber(2)
                                            .PageSize(5)
                                            .ExecuteAsync();

        Assert.AreEqual(2, PageCount);
        Assert.IsTrue(Results.Count > 0);
    }

    class BookResult
    {
        public string BookTitle { get; set; }
        public ObjectId? BookID { get; set; }
    }

    [TestMethod]
    public async Task with_projection()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

         await DB.Instance()
              .PagedSearch<BookObjectId, BookResult>()
              .Match(b => b.Title == guid)
              .Sort(b => b.ID, Order.Ascending)
              .Project(b => new() { BookID = b.ID, BookTitle = b.Title })
              .PageNumber(1)
              .PageSize(5)
              .ExecuteAsync();
    }

    [TestMethod]
    public async Task sort_by_meta_text_score_with_projection()
    {
        await DB.Instance().DropCollectionAsync<GenreObjectId>();

        await DB.Instance().Index<GenreObjectId>()
                .Key(g => g.Name, KeyType.Text)
                .Option(o => o.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid();

        var list = new[]
        {
            new GenreObjectId { GuidID = guid, Position = 0, Name = "this should not match" },
            new GenreObjectId { GuidID = guid, Position = 3, Name = "one two three four five six" },
            new GenreObjectId { GuidID = guid, Position = 4, Name = "one two three four five six seven" },
            new GenreObjectId { GuidID = guid, Position = 2, Name = "one two three four five six seven eight" },
            new GenreObjectId { GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine" }
        };

        await list.SaveAsync();

        var (Results, _, _) =  await DB.Instance()
                                    .PagedSearch<GenreObjectId>()
                                    .Match(Search.Full, "one eight nine")
                                    .Project(p => new() { Name = p.Name, Position = p.Position })
                                    .SortByTextScore()
                                    .ExecuteAsync();

        Assert.AreEqual(4, Results.Count);
        Assert.AreEqual(1, Results[0].Position);
        Assert.AreEqual(4, Results[Results.Count - 1].Position);
    }

    [TestMethod]
    public async Task sort_by_meta_text_score_no_projection()
    {
        await DB.Instance().DropCollectionAsync<GenreObjectId>();

        await DB.Instance().Index<GenreObjectId>()
                .Key(g => g.Name, KeyType.Text)
                .Option(o => o.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid();

        var list = new[]
        {
            new GenreObjectId { GuidID = guid, Position = 0, Name = "this should not match" },
            new GenreObjectId { GuidID = guid, Position = 3, Name = "one two three four five six" },
            new GenreObjectId { GuidID = guid, Position = 4, Name = "one two three four five six seven" },
            new GenreObjectId { GuidID = guid, Position = 2, Name = "one two three four five six seven eight" },
            new GenreObjectId { GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine" }
        };

        await list.SaveAsync();

        var (Results, _, _) =  await DB.Instance()
                                    .PagedSearch<GenreObjectId>()
                                    .Match(Search.Full, "one eight nine")
                                    .SortByTextScore()
                                    .ExecuteAsync();

        Assert.AreEqual(4, Results.Count);
        Assert.AreEqual(1, Results[0].Position);
        Assert.AreEqual(4, Results[Results.Count - 1].Position);
    }

    [TestMethod]
    public async Task exclusion_projection_works()
    {
        var author = new AuthorObjectId
        {
            Name = "name",
            Surname = "sername",
            Age = 22,
            FullName = "fullname"
        };
        await author.SaveAsync();

        var (res, _, _) = await DB.Instance().PagedSearch<AuthorObjectId>()
                                  .Match(a => a.ID == author.ID)
                                  .Sort(a => a.ID, Order.Ascending)
                                  .ProjectExcluding(a => new { a.Age, a.Name })
                                  .ExecuteAsync();

        Assert.AreEqual(author.FullName, res[0].FullName);
        Assert.AreEqual(author.Surname, res[0].Surname);
        Assert.IsTrue(res[0].Age == default);
        Assert.IsTrue(res[0].Name == default);
    }
}