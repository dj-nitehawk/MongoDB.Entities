using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class CountingUuid
{
    DBContext db;

    Task Init(string guid)
    {
        db = new MyDBUuid();

        var list = new List<AuthorUuid>();

        for (var i = 1; i <= 25; i++)
        {
            list.Add(new AuthorUuid { Name = guid, Age = 111 });
        }

        for (var i = 1; i <= 10; i++)
        {
            list.Add(new AuthorUuid { Name = guid, Age = 222 });
        }

        return list.SaveAsync();
    }

    [TestMethod]
    public async Task count_estimated_works()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountEstimatedAsync<AuthorUuid>();

        Assert.IsTrue(count > 0);
    }

    [TestMethod]
    public async Task count_with_lambda()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorUuid>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_lambda_use_json_string_filter()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorUuid>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_definition()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var filter = DB.Filter<AuthorUuid>()
                        .Eq(a => a.Name, guid);

        var count = await db.CountAsync(filter);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_builder()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorUuid>(b => b.Eq(a => a.Name, guid));

        Assert.AreEqual(25, count);
    }
}
