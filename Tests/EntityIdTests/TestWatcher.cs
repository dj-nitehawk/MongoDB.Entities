using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class Watcher
{
    [TestMethod]
    public async Task watching_works()
    {
        var watcher = DB.Watcher<FlowerEntity>("test");
        var allFlowers = new List<FlowerEntity>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            f => f.FullDocument.Name == "test");

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await new[] {
            new FlowerEntity { Name = "test" },
            new FlowerEntity { Name = "test" },
            new FlowerEntity { Name = "test" }
        }.SaveAsync();

        var flower = new FlowerEntity { Name = "test" };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_projection_works()
    {
        var watcher = DB.Watcher<FlowerEntity>("test-with-projection");
        var allFlowers = new List<FlowerEntity>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            f => new FlowerEntity { Color = f.Color, NestedFlower = f.NestedFlower },
            f => f.FullDocument.Color == "red");

        await Task.Delay(500);

        watcher.OnChangesAsync += async flowers =>
        {
            allFlowers.AddRange(flowers);
            await Task.CompletedTask;
        };

        await new[] {
            new FlowerEntity { Name = "test", Color = "red", NestedFlower = new() {Name = "nested" } },
            new FlowerEntity { Name = "test", Color = "red" },
            new FlowerEntity { Name = "test", Color = "red" }
        }.SaveAsync();

        var flower = new FlowerEntity { Name = "test" };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(3, allFlowers.Count);
        Assert.IsTrue(
            allFlowers[0].Name == null &&
            allFlowers[0].Color == "red" &&
            allFlowers[0].NestedFlower.Name == "nested");
    }

    [TestMethod]
    public async Task watching_with_filter_builders()
    {
        var guid = Guid.NewGuid().ToString();

        var watcher = DB.Watcher<FlowerEntity>("test-with-filter-builders");
        var allFlowers = new List<FlowerEntity>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            b => b.Eq(d => d.FullDocument.Name, guid));

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await new[] {
            new FlowerEntity { Name = guid },
            new FlowerEntity { Name = guid },
            new FlowerEntity { Name = guid }
        }.SaveAsync();

        var flower = new FlowerEntity { Name = guid };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_filter_builders_CSD()
    {
        var guid = Guid.NewGuid().ToString();

        var watcher = DB.Watcher<FlowerEntity>("test-with-filter-builders-csd");
        var allFlowers = new List<FlowerEntity>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            b => b.Eq(d => d.FullDocument.Name, guid));

        await Task.Delay(500);

        watcher.OnChangesCSDAsync += async csDocs =>
        {
            allFlowers.AddRange(csDocs.Select(x => x.FullDocument));
            await Task.CompletedTask;
        };

        await new[] {
            new FlowerEntity { Name = guid },
            new FlowerEntity { Name = "exclude me" },
            new FlowerEntity { Name = guid },
            new FlowerEntity { Name = guid },
        }.SaveAsync();

        var flower = new FlowerEntity { Name = guid };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }
}
