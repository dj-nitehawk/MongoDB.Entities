using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public class GeoNearEntityTest
{
    [TestMethod]
    public async Task find_match_geo_method()
    {
        var db = DB.Default;

        await db.Index<PlaceEntity>()
                .Key(x => x.Location, KeyType.Geo2DSphere)
                .Option(x => x.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid().ToString();

        await db.SaveAsync(
        [
            new() { Name = "Paris " + guid, Location = new(48.8539241, 2.2913515) },
            new() { Name = "Versailles " + guid, Location = new(48.796964, 2.137456) },
            new PlaceEntity { Name = "Poissy " + guid, Location = new(48.928860, 2.046889) }
        ]);

        var res = (await db.Find<PlaceEntity>()
                           .Match(p => p.Location, new(48.857908, 2.295243), 20000) //20km from eiffel tower
                           .Sort(p => p.ModifiedOn, Order.Descending)
                           .Limit(20)
                           .ExecuteAsync())
                  .Where(c => c.Name == "Paris " + guid)
                  .ToList();

        Assert.HasCount(1, res);
    }

    [TestMethod]
    public async Task geo_near_fluent_interface()
    {
        var db = DB.Default;

        await db.Index<PlaceEntity>()
                .Key(x => x.Location, KeyType.Geo2DSphere)
                .Option(x => x.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid().ToString();

        await db.SaveAsync(
        [
            new() { Name = "Paris " + guid, Location = new(48.8539241, 2.2913515) },
            new() { Name = "Versailles " + guid, Location = new(48.796964, 2.137456) },
            new PlaceEntity { Name = "Poissy " + guid, Location = new(48.928860, 2.046889) }
        ]);

        var qry = db.GeoNear<PlaceEntity>(
            NearCoordinates: new(48.857908, 2.295243), //eiffel tower
            DistanceField: x => x.DistanceKM,
            MaxDistance: 20000);

        var cnt = await qry.Match(c => c.Name.Contains(guid)).ToListAsync();
        Assert.HasCount(2, cnt);

        var res = await qry.Match(c => c.Name == "Paris " + guid).ToListAsync();
        Assert.HasCount(1, res);
    }

    [TestMethod]
    public async Task geo_near_transaction_returns_correct_results()
    {
        var db = DB.Default;

        await db.Index<PlaceEntity>()
                .Key(x => x.Location, KeyType.Geo2DSphere)
                .Option(x => x.Background = false)
                .CreateAsync();

        var guid = Guid.NewGuid().ToString();

        using var tn = db.Transaction();

        await db.SaveAsync(
        [
            new() { Name = "Paris " + guid, Location = new(48.8539241, 2.2913515) },
            new() { Name = "Versailles " + guid, Location = new(48.796964, 2.137456) },
            new PlaceEntity { Name = "Poissy " + guid, Location = new(48.928860, 2.046889) }
        ]);

        var qry = tn.GeoNear<PlaceEntity>(
            NearCoordinates: new(48.857908, 2.295243), //eiffel tower
            DistanceField: x => x.DistanceKM,
            MaxDistance: 20000);

        var cnt = await qry.Match(c => c.Name.Contains(guid)).ToListAsync();
        Assert.HasCount(2, cnt);

        var res = await qry.Match(c => c.Name == "Paris " + guid).ToListAsync();
        Assert.HasCount(1, res);
    }
}