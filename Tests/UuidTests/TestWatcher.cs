using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class WatcherUuid
{
    [TestMethod]
    public async Task watching_works()
    {
        var watcher = DB.Default.Watcher<FlowerUuid>("test");
        var allFlowers = new List<FlowerUuid>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            f => f.FullDocument.Name == "test");

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await new[] {
            new FlowerUuid { Name = "test" },
            new FlowerUuid { Name = "test" },
            new FlowerUuid { Name = "test" }
        }.SaveAsync();

        var flower = new FlowerUuid { Name = "test" };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_projection_works()
    {
        var db = DB.Default;
        
        var watcher = db.Watcher<FlowerUuid>("test-with-projection");
        var allFlowers = new List<FlowerUuid>();

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
            new FlowerUuid { Name = "test", Color = "red", NestedFlower = new() {Name = "nested" } },
            new FlowerUuid { Name = "test", Color = "red" },
            new FlowerUuid { Name = "test", Color = "red" }
        }.SaveAsync(db);

        var flower = new FlowerUuid { Name = "test" };
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

        var watcher = DB.Default.Watcher<FlowerUuid>("test-with-filter-builders");
        var allFlowers = new List<FlowerUuid>();

        watcher.Start(
            EventType.Created | EventType.Updated,
            b => b.Eq(d => d.FullDocument.Name, guid));

        await Task.Delay(500);

        watcher.OnChanges +=
            allFlowers.AddRange;

        await new[] {
            new FlowerUuid { Name = guid },
            new FlowerUuid { Name = guid },
            new FlowerUuid { Name = guid }
        }.SaveAsync();

        var flower = new FlowerUuid { Name = guid };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }

    [TestMethod]
    public async Task watching_with_filter_builders_CSD()
    {
        var guid = Guid.NewGuid().ToString();

        var watcher = DB.Default.Watcher<FlowerUuid>("test-with-filter-builders-csd");
        var allFlowers = new List<FlowerUuid>();

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
            new FlowerUuid { Name = guid },
            new FlowerUuid { Name = "exclude me" },
            new FlowerUuid { Name = guid },
            new FlowerUuid { Name = guid },
        }.SaveAsync();

        var flower = new FlowerUuid { Name = guid };
        await flower.SaveAsync();

        await flower.DeleteAsync();

        await Task.Delay(500);

        Assert.AreEqual(4, allFlowers.Count);
    }
}