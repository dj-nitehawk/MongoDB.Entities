using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class WatcherEntity
{
    readonly DB db = DB.Default;

    [TestMethod]
    public async Task watching_works()
    {
        var watcher = db.Watcher<FlowerEntity>("test");
        var allFlowers = new List<FlowerEntity>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            f => f.FullDocument.Name == "test");

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await db.SaveAsync(
        [
            new() { Name = "test" },
            new() { Name = "test" },
            new FlowerEntity { Name = "test" }
        ]);

        var flower = new FlowerEntity { Name = "test" };
        await db.SaveAsync(flower);
        await db.DeleteAsync(flower);

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_projection_works()
    {
        var watcher = db.Watcher<FlowerEntity>("test-with-projection");
        var allFlowers = new List<FlowerEntity>();

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

        await db.SaveAsync(
        [
            new() { Name = "test", Color = "red", NestedFlower = new() { Name = "nested" } },
            new() { Name = "test", Color = "red" },
            new FlowerEntity { Name = "test", Color = "red" }
        ]);

        var flower = new FlowerEntity { Name = "test" };
        await db.SaveAsync(flower);
        await db.DeleteAsync(flower);

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

        var watcher = db.Watcher<FlowerEntity>("test-with-filter-builders");
        var allFlowers = new List<FlowerEntity>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            b => b.Eq(d => d.FullDocument.Name, guid));

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await db.SaveAsync(
        [
            new() { Name = guid },
            new() { Name = guid },
            new FlowerEntity { Name = guid }
        ]);

        var flower = new FlowerEntity { Name = guid };
        await db.SaveAsync(flower);
        await db.DeleteAsync(flower);
        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_filter_builders_CSD()
    {
        var guid = Guid.NewGuid().ToString();

        var watcher = db.Watcher<FlowerEntity>("test-with-filter-builders-csd");
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

        await db.SaveAsync(
        [
            new() { Name = guid },
            new() { Name = "exclude me" },
            new() { Name = guid },
            new FlowerEntity { Name = guid }
        ]);

        var flower = new FlowerEntity { Name = guid };
        await db.SaveAsync(flower);
        await db.DeleteAsync(flower);
        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }
}