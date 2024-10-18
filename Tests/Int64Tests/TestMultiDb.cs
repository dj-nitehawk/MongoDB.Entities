using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class MultiDbInt64
{
    const string dbName = "mongodb-entities-test-multi";

    [TestMethod]
    public async Task save_Int64_works()
    {
        await DB.InitAsync(dbName);

        DB.DatabaseFor<BookCover>(dbName);
        DB.DatabaseFor<BookMark>(dbName);

        var cover = new BookCover
        {
            BookID = "123",
            BookName = "test book " + Guid.NewGuid()
        };

        await cover.SaveAsync();
        Assert.IsNotNull(cover.ID);

        var res = await DB.Find<BookCover>().OneAsync(cover.ID);

        Assert.AreEqual(cover.ID, res!.ID);
        Assert.AreEqual(cover.BookName, res.BookName);
    }

    [TestMethod]
    public async Task relationships_work()
    {
        await DB.InitAsync(dbName);
        DB.DatabaseFor<BookCover>(dbName);
        DB.DatabaseFor<BookMark>(dbName);

        var cover = new BookCover
        {
            BookID = "123",
            BookName = "test book " + Guid.NewGuid()
        };
        await cover.SaveAsync();

        var mark = new BookMark
        {
            BookCover = cover.ToReference(),
            BookName = cover.BookName
        };

        await mark.SaveAsync();

        await cover.BookMarks.AddAsync(mark);

        var res = await cover.BookMarks.ChildrenQueryable().FirstAsync();

        Assert.AreEqual(cover.BookName, res.BookName);

        Assert.AreEqual((await res.BookCover.ToEntityAsync()).ID, cover.ID);
    }

    [TestMethod]
    public async Task get_instance_by_db_name()
    {
        await DB.InitAsync("test1");
        await DB.InitAsync("test2");

        var res = DB.Database("test2");

        Assert.AreEqual("test2", res.DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public void uninitialized_get_instance_throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() => DB.Database("some-database"));
    }

    [TestMethod]
    public async Task multiple_initializations_should_not_throw()
    {
        await DB.InitAsync("multi-init");
        await DB.InitAsync("multi-init");

        var db = DB.Database("multi-init");

        Assert.AreEqual("multi-init", db.DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public async Task dropping_collections()
    {
        await DB.InitAsync(dbName);
        DB.DatabaseFor<BookMark>(dbName);
        DB.DatabaseFor<BookCover>(dbName);

        var guid = Guid.NewGuid().ToString();
        var marks = new[]
        {
            new BookMark { BookName = guid },
            new BookMark { BookName = guid },
            new BookMark { BookName = guid }
        };

        await marks.SaveAsync();

        var covers = new[]
        {
            new BookCover { BookID = guid },
            new BookCover { BookID = guid },
            new BookCover { BookID = guid }
        };

        await covers.SaveAsync();

        foreach (var cover in covers)
            await cover.BookMarks.AddAsync(marks);

        Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == marks.Length));

        await DB.DropCollectionAsync<BookMark>();

        Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == 0));

        Assert.AreEqual(3, DB.Queryable<BookCover>().Where(b => b.BookID == guid).Count());
    }

    [TestMethod]
    public async Task dbcontext_ctor_connections()
    {
        var db = new DBContext(dbName, "localhost", modifiedBy: new());

        var author = new AuthorInt64 { Name = "test" };
        await db.SaveAsync(author);

        var res = await db.Find<AuthorInt64>().OneAsync(author.ID);

        Assert.AreEqual(author.ID, res!.ID);
    }
}