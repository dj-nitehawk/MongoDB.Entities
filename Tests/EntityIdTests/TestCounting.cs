using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class CountingEntity
{
    DB _db = null!;

    Task Init(string guid)
    {
        _db = new MyDbEntity();

        var list = new List<AuthorEntity>();

        for (var i = 1; i <= 25; i++)
            list.Add(new() { Name = guid, Age = 111 });

        for (var i = 1; i <= 10; i++)
            list.Add(new() { Name = guid, Age = 222 });

        return _db.SaveAsync(list);
    }

    [TestMethod]
    public async Task count_estimated_works()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await _db.CountEstimatedAsync<AuthorEntity>();

        Assert.IsGreaterThan(0, count);
    }

    [TestMethod]
    public async Task count_with_lambda()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await _db.CountAsync<AuthorEntity>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_lambda_use_json_string_filter()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await _db.CountAsync<AuthorEntity>(a => a.Name == guid);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_definition()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var filter = DB.Filter<AuthorEntity>()
                       .Eq(a => a.Name, guid);

        var count = await _db.CountAsync(filter);

        Assert.AreEqual(25, count);
    }

    [TestMethod]
    public async Task count_with_filter_builder()
    {
        var guid = Guid.NewGuid().ToString();
        await Init(guid);

        var count = await _db.CountAsync<AuthorEntity>(b => b.Eq(a => a.Name, guid));

        Assert.AreEqual(25, count);
    }
}