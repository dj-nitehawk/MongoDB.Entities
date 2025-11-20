using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class WatcherObjectId
{
    [TestMethod]
    public async Task watching_works()
    {
        var watcher = DB.Default.Watcher<FlowerObjectId>("test");
        var allFlowers = new List<FlowerObjectId>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            f => f.FullDocument.Name == "test");

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await new[] {
            new FlowerObjectId { Name = "test" },
            new FlowerObjectId { Name = "test" },
            new FlowerObjectId { Name = "test" }
        }.SaveAsync();

        var flower = new FlowerObjectId { Name = "test" };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_projection_works()
    {
        var db = DB.Default;
        var watcher = db.Watcher<FlowerObjectId>("test-with-projection");
        var allFlowers = new List<FlowerObjectId>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            f => new() { Color = f.Color, NestedFlower = f.NestedFlower },
            f => f.FullDocument.Color == "red");

        await Task.Delay(500);

        watcher.OnChangesAsync += async flowers =>
        {
            allFlowers.AddRange(flowers);
            await Task.CompletedTask;
        };

        await new[] {
            new FlowerObjectId { Name = "test", Color = "red", NestedFlower = new() {Name = "nested" } },
            new FlowerObjectId { Name = "test", Color = "red" },
            new FlowerObjectId { Name = "test", Color = "red" }
        }.SaveAsync(db);

        var flower = new FlowerObjectId { Name = "test" };
        await flower.SaveAsync(db);

        await flower.DeleteAsync(db);

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

        var watcher = DB.Default.Watcher<FlowerObjectId>("test-with-filter-builders");
        var allFlowers = new List<FlowerObjectId>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            b => b.Eq(d => d.FullDocument.Name, guid));

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await new[] {
            new FlowerObjectId { Name = guid },
            new FlowerObjectId { Name = guid },
            new FlowerObjectId { Name = guid }
        }.SaveAsync();

        var flower = new FlowerObjectId { Name = guid };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_filter_builders_CSD()
    {
        var guid = Guid.NewGuid().ToString();

        var watcher = DB.Default.Watcher<FlowerObjectId>("test-with-filter-builders-csd");
        var allFlowers = new List<FlowerObjectId>();

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
            new FlowerObjectId { Name = guid },
            new FlowerObjectId { Name = "exclude me" },
            new FlowerObjectId { Name = guid },
            new FlowerObjectId { Name = guid },
        }.SaveAsync();

        var flower = new FlowerObjectId { Name = guid };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }
}