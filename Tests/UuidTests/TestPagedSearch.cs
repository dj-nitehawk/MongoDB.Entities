using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class PagedSearchUuid
{
    [TestMethod]
    public async Task empty_results()
    {
        var guid = Guid.Empty.ToString();

        var (Results, _, PageCount) =  await DBInstance.Instance()
                                            .PagedSearch<BookUuid>()
                                            .Match(b => b.ID == guid)
                                            .Sort(b => b.ID, Order.Ascending)
                                            .PageNumber(1)
                                            .PageSize(200)
                                            .ExecuteAsync();

        Assert.AreEqual(0, PageCount);
        Assert.IsTrue(Results.Count == 0);
    }

    static Task SeedData(string guid)
    {
        var list = new List<BookUuid>(10);

        for (var i = 1; i <= 10; i++)
            list.Add(new() { Title = guid });

        return list.SaveAsync();
    }

    [TestMethod]
    public async Task got_results()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        var (Results, _, PageCount) = await DBInstance.Instance()
                                                      .PagedSearch<BookUuid>()
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

        var (Results, _, PageCount) = await DBInstance.Instance()
                                                      .PagedSearch<BookUuid>()
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

        var pipeline = DBInstance.Instance().Fluent<BookUuid>()
                         .Match(b => b.Title == guid);

        var (Results, _, PageCount) = await DBInstance.Instance()
                                                      .PagedSearch<BookUuid>()
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
        public string? BookID { get; set; }
    }

    [TestMethod]
    public async Task with_projection()
    {
        var guid = Guid.NewGuid().ToString();

        await SeedData(guid);

        await DBInstance.Instance()
                        .PagedSearch<BookUuid, BookResult>()
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
        await DBInstance.Instance().DropCollectionAsync<GenreUuid>();

        await DBInstance.Instance().Index<GenreUuid>()
                .Key(g => g.Name, KeyType.Text)
                .Option(o => o.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid();

        var list = new[]
        {
            new GenreUuid { GuidID = guid, Position = 0, Name = "this should not match" },
            new GenreUuid { GuidID = guid, Position = 3, Name = "one two three four five six" },
            new GenreUuid { GuidID = guid, Position = 4, Name = "one two three four five six seven" },
            new GenreUuid { GuidID = guid, Position = 2, Name = "one two three four five six seven eight" },
            new GenreUuid { GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine" }
        };

        await list.SaveAsync();

        var (Results, _, _) = await DBInstance.Instance()
                                              .PagedSearch<GenreUuid>()
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
        await DBInstance.Instance().DropCollectionAsync<GenreUuid>();

        await DBInstance.Instance().Index<GenreUuid>()
                .Key(g => g.Name, KeyType.Text)
                .Option(o => o.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid();

        var list = new[]
        {
            new GenreUuid { GuidID = guid, Position = 0, Name = "this should not match" },
            new GenreUuid { GuidID = guid, Position = 3, Name = "one two three four five six" },
            new GenreUuid { GuidID = guid, Position = 4, Name = "one two three four five six seven" },
            new GenreUuid { GuidID = guid, Position = 2, Name = "one two three four five six seven eight" },
            new GenreUuid { GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine" }
        };

        await list.SaveAsync();

        var (Results, _, _) = await DBInstance.Instance()
                                              .PagedSearch<GenreUuid>()
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
        var author = new AuthorUuid
        {
            Name = "name",
            Surname = "sername",
            Age = 22,
            FullName = "fullname"
        };
        await author.SaveAsync();

        var (res, _, _) = await DBInstance.Instance().PagedSearch<AuthorUuid>()
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