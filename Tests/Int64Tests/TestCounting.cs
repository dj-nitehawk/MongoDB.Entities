using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class CountingInt64
{
    DBContext db;

    Task Init(string InitialName)
    {
        db = new MyDBInt64();

        var list = new List<AuthorInt64>();

        for (var i = 1; i <= 25; i++)
        {
            list.Add(new() { Name = InitialName, Age = 111 });
        }

        for (var i = 1; i <= 10; i++)
        {
            list.Add(new() { Name = InitialName, Age = 222 });
        }

        return list.SaveAsync();
    }

    [TestMethod]
    public async Task count_estimated_works()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountEstimatedAsync<AuthorInt64>();

        Assert.IsTrue(count > 0);
    }
    
    [TestMethod]
    public async Task count_with_lambda()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorInt64>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_lambda_use_json_string_filter()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorInt64>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_definition()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var filter = DBInstance.Instance().Filter<AuthorInt64>()
                        .Eq(a => a.Name, guid);

        var count = await db.CountAsync(filter);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_builder()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await db.CountAsync<AuthorInt64>(b => b.Eq(a => a.Name, guid));

        Assert.AreEqual(25, count);
    }
}
