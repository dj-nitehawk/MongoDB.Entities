using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ModifiedByInt64
{
    [TestMethod]
    public async Task throw_if_mod_by_not_supplied()
    {
        var db = new DBContext();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await db.SaveAsync(new AuthorInt64()));
    }

    [TestMethod]
    public async Task base_mod_by_save()
    {
        var userID = ObjectId.GenerateNewId().ToString();

        var db = new DBContext(
            modifiedBy: new()
            {
                UserID = userID,
                UserName = "TestUser"
            });

        var author = new AuthorInt64();
        await db.SaveAsync(author);

        var res = await db.Find<AuthorInt64>().OneAsync(author.ID);

        Assert.AreEqual(res!.UpdatedBy.UserID, userID);
        Assert.AreEqual(res.UpdatedBy.UserName, "TestUser");
    }

    [TestMethod]
    public async Task derived_mod_by_save()
    {
        var userID = ObjectId.GenerateNewId().ToString();

        var db = new DBContext(
            modifiedBy: new UpdatedBy
            {
                UserID = userID,
                UserName = "TestUser",
                UserType = "TEST"
            });

        var author = new BookInt64();
        await db.SaveAsync(author);

        var res = await db.Find<BookInt64>().OneAsync(author.ID);

        Assert.AreEqual(res!.ModifiedBy.UserID, userID);
        Assert.AreEqual(res.ModifiedBy.UserName, "TestUser");
        Assert.AreEqual(res.ModifiedBy.UserType, "TEST");
    }

    [TestMethod]
    public async Task mod_by_replace()
    {
        var userID = ObjectId.GenerateNewId().ToString();
        var db = new DBContext(
            modifiedBy: new UpdatedBy
            {
                UserID = userID,
                UserName = "TestUser",
                UserType = "TEST"
            });
        var book = new BookInt64();
        await db.SaveAsync(book);

        userID = ObjectId.GenerateNewId().ToString();
        db.ModifiedBy = new UpdatedBy
        {
            UserID = userID,
            UserName = "TestUserUPDATED",
            UserType = "TEST-UPDATED"
        };

        book.Title = "TEST().BOOK";

        await db
            .Replace<BookInt64>()
            .MatchID(book.ID)
            .WithEntity(book)
            .ExecuteAsync();

        var res = await db.Find<BookInt64>().OneAsync(book.ID);

        Assert.AreEqual(res!.ModifiedBy.UserID, userID);
        Assert.AreEqual(res.ModifiedBy.UserName, "TestUserUPDATED");
        Assert.AreEqual(res.ModifiedBy.UserType, "TEST-UPDATED");
        Assert.AreEqual(res.Title, "TEST().BOOK");
    }

    [TestMethod]
    public async Task mod_by_update()
    {
        var userID = ObjectId.GenerateNewId().ToString();
        var db = new DBContext(
            modifiedBy: new UpdatedBy
            {
                UserID = userID,
                UserName = "TestUser",
                UserType = "TEST"
            });
        var book = new BookInt64();
        await db.SaveAsync(book);

        userID = ObjectId.GenerateNewId().ToString();
        db.ModifiedBy = new UpdatedBy
        {
            UserID = userID,
            UserName = "TestUserUPDATED",
            UserType = "TEST-UPDATED"
        };
        await db
            .Update<BookInt64>()
            .MatchID(book.ID)
            .Modify(b => b.Title, "TEST().BOOK")
            .ExecuteAsync();

        var res = await db.Find<BookInt64>().OneAsync(book.ID);

        Assert.AreEqual(res!.ModifiedBy.UserID, userID);
        Assert.AreEqual(res.ModifiedBy.UserName, "TestUserUPDATED");
        Assert.AreEqual(res.ModifiedBy.UserType, "TEST-UPDATED");
        Assert.AreEqual(res.Title, "TEST().BOOK");
    }

    [TestMethod]
    public async Task mod_by_update_using_modifyonly()
    {
        var userID = ObjectId.GenerateNewId().ToString();
        var db = new DBContext(
            modifiedBy: new UpdatedBy
            {
                UserID = userID,
                UserName = "TestUser",
                UserType = "TEST"
            });
        var book = new BookInt64();
        await db.SaveAsync(book);

        userID = ObjectId.GenerateNewId().ToString();
        db.ModifiedBy = new UpdatedBy
        {
            UserID = userID,
            UserName = "TestUserUPDATED",
            UserType = "TEST-UPDATED"
        };

        book.Title = "TEST().BOOK";

        await db
            .Update<BookInt64>()
            .MatchID(book.ID)
            .ModifyOnly(x => new { x.Title }, book)
            .ExecuteAsync();

        var res = await db.Find<BookInt64>().OneAsync(book.ID);

        Assert.AreEqual(res!.ModifiedBy.UserID, userID);
        Assert.AreEqual(res.ModifiedBy.UserName, "TestUserUPDATED");
        Assert.AreEqual(res.ModifiedBy.UserType, "TEST-UPDATED");
        Assert.AreEqual(res.Title, "TEST().BOOK");
    }
}
