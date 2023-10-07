using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class CountingObjectId
{
    DBContext db;

    Task Init(string ObjectId)
    {
        db = new MyDBObjectId();

        var list = new List<AuthorObjectId>();

        for (int i = 1; i <= 25; i++)
        {
            list.Add(new AuthorObjectId { Name = ObjectId, Age = 111 });
        }

        for (int i = 1; i <= 10; i++)
        {
            list.Add(new AuthorObjectId { Name = ObjectId, Age = 222 });
        }

        return list.SaveAsync();
    }

    [TestMethod]
    public async Task count_estimated_works()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountEstimatedAsync<AuthorObjectId>();

        Assert.IsTrue(count > 0);
    }

    [TestMethod]
    public async Task count_with_lambda()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorObjectId>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_lambda_use_json_string_filter()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorObjectId>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_definition()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var filter = DB.Filter<AuthorObjectId>()
                        .Eq(a => a.Name, guid);

        var count = await db.CountAsync(filter);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_builder()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorObjectId>(b => b.Eq(a => a.Name, guid));

        Assert.AreEqual(25, count);
    }
}
