using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DatesEntity
{
    [TestMethod]
    public async Task not_setting_date_doesnt_cause_issuesAsync()
    {
        var book = new BookEntity { Title = "nsddci" };
        await book.SaveAsync();

        var res = await DB.Instance().Find<BookEntity>().OneAsync(book.ID);

        Assert.AreEqual(res!.Title, book.Title);
        Assert.IsNull(res.PublishedOn);
    }

    [TestMethod]
    public async Task date_props_contain_correct_value()
    {
        var pubDate = DateTime.UtcNow;

        var book = new BookEntity
        {
            Title = "dpccv",
            PublishedOn = pubDate.ToDate()
        };
        await book.SaveAsync();

        var res = await DB.Instance().Find<BookEntity>().OneAsync(book.ID);

        Assert.AreEqual(pubDate.Ticks, res!.PublishedOn!.Ticks);
        Assert.AreEqual(pubDate.ToUniversalTime(), res.PublishedOn.DateTime);
        Assert.AreEqual(pubDate, res.PublishedOn.DateTime);
        Assert.AreEqual(DateTimeKind.Utc, res.PublishedOn.DateTime.Kind);
    }

    [TestMethod]
    public async Task querying_on_ticks_when_null()
    {
        var book = new BookEntity
        {
            Title = "qotwn",
        };
        await book.SaveAsync();

        var res = await DB.Instance().Queryable<BookEntity>()
                                  .Where(b => b.ID == book.ID && b.PublishedOn!.Ticks > 0)
                                  .SingleOrDefaultAsync();

        Assert.IsNull(res);
    }

    [TestMethod]
    public async Task querying_on_ticks_work()
    {
        var pubDate = DateTime.UtcNow;

        var book = new BookEntity
        {
            Title = "qotw",
            PublishedOn = new(pubDate)
        };
        await book.SaveAsync();

        var db = DB.Instance();
        
        var res = (await db.Find<BookEntity>()
                                   .Match(b => b.ID == book.ID && b.PublishedOn!.Ticks == pubDate.Ticks)
                                   .ExecuteAsync())
                    .Single();

        Assert.AreEqual(book.ID, res.ID);

        res = (await db.Find<BookEntity>()
                               .Match(b => b.ID == book.ID && b.PublishedOn!.Ticks < pubDate.Ticks + TimeSpan.FromSeconds(1).Ticks)
                               .ExecuteAsync())
                .Single();

        Assert.AreEqual(book.ID, res.ID);
    }

    [TestMethod]
    public async Task querying_on_datetime_prop_works()
    {
        var pubDate = DateTime.UtcNow;

        var book = new BookEntity
        {
            Title = "qodtpw",
            PublishedOn = new(pubDate)
        };
        await book.SaveAsync();

        var db = DB.Instance();
        
        var res = (await db.Find<BookEntity>()
                                   .Match(b => b.ID == book.ID && b.PublishedOn!.DateTime == pubDate)
                                   .ExecuteAsync())
        .Single();

        Assert.AreEqual(book.ID, res.ID);

        res = (await db.Find<BookEntity>()
                               .Match(b => b.ID == book.ID && b.PublishedOn!.DateTime < pubDate.AddSeconds(1))
                               .ExecuteAsync())
                .Single();

        Assert.AreEqual(book.ID, res.ID);
    }

    [TestMethod]
    public void setting_ticks_creates_correct_datetime()
    {
        var now = DateTime.UtcNow;

        var date = new Date() { Ticks = now.Ticks };

        Assert.AreEqual(now, date.DateTime);
    }
}