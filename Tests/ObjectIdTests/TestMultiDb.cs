using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class MultiDbObjectId
{
    const string dbName = "mongodb-entities-test-multi";

    [TestMethod]
    public async Task save_ObjectId_works()
    {
        var db = await DB.InitAsync(dbName);

        var cover = new BookCover
        {
            BookID = "123",
            BookName = "test book " + Guid.NewGuid()
        };

        await cover.SaveAsync(db);
        Assert.IsNotNull(cover.ID);

        var res = await db.Find<BookCover>().OneAsync(cover.ID);

        Assert.AreEqual(cover.ID, res!.ID);
        Assert.AreEqual(cover.BookName, res.BookName);

        res = await DB.Instance().Find<BookCover>().OneAsync(cover.ID);
        Assert.IsNull(res);
    }

    [TestMethod]
    public async Task relationships_work()
    {
        var db = await DB.InitAsync(dbName);

        var cover = new BookCover(db)
        {
            BookID = "123",
            BookName = "test book " + Guid.NewGuid()
        };
        await cover.SaveAsync(db);

        var mark = new BookMark
        {
            BookCover = cover.ToReference(),
            BookName = cover.BookName
        };

        await mark.SaveAsync(db);

        await cover.BookMarks.AddAsync(mark);

        var res = await cover.BookMarks.ChildrenQueryable().FirstAsync();

        Assert.AreEqual(cover.BookName, res.BookName);

        Assert.AreEqual((await res.BookCover.ToEntityAsync(db)).ID, cover.ID);
    }

    [TestMethod]
    public async Task get_instance_by_db_name()
    {
        var db1 = await DB.InitAsync("test1");
        var db2 = await DB.InitAsync("test2");

        var res = DB.Instance("test2").Database();

        Assert.AreEqual("test2", res.DatabaseNamespace.DatabaseName);
        Assert.AreEqual("test2", db2.Database().DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public void uninitialized_get_instance_throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() => DB.Instance("some-database").Database());
    }

    [TestMethod]
    public async Task multiple_initializations_should_not_throw()
    {
        await  DB.InitAsync("multi-init");
        await  DB.InitAsync("multi-init");

        var db = DB.Instance("multi-init").Database();

        Assert.AreEqual("multi-init", db.DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public async Task dropping_collections()
    {
        var db = await DB.InitAsync(dbName);

        var guid = Guid.NewGuid().ToString();
        var marks = new[]
        {
            new BookMark { BookName = guid },
            new BookMark { BookName = guid },
            new BookMark { BookName = guid }
        };

        await marks.SaveAsync(db);

        var covers = new[]
        {
            new BookCover(db) { BookID = guid },
            new BookCover(db) { BookID = guid },
            new BookCover(db) { BookID = guid }
        };

        await covers.SaveAsync(db);

        foreach (var cover in covers)
            await cover.BookMarks.AddAsync(marks);

        Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == marks.Length));

        await db.DropCollectionAsync<BookMark>();

        Assert.IsTrue(covers.Select(b => b.BookMarks.Count()).All(x => x == 0));

        Assert.AreEqual(3, db.Queryable<BookCover>().Where(b => b.BookID == guid).Count());
    }

    [TestMethod]
    public async Task dbcontext_ctor_connections()
    {
        var db = new DBContext(dbName, "localhost", modifiedBy: new());

        var author = new AuthorObjectId { Name = "test" };
        await db.SaveAsync(author);

        var res = await db.Find<AuthorObjectId>().OneAsync(author.ID);

        Assert.AreEqual(author.ID, res!.ID);
    }
}