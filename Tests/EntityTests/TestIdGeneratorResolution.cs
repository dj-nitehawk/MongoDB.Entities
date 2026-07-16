using System;
using MongoDB.Bson;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class IdGeneratorResolution
{
    readonly DB _db = DB.Default;

    [TestMethod]
    public async Task cache_and_manual_id_work_without_generator()
    {
        var entity = new NoGeneratorIntEntity
        {
            ID = 4242,
            Name = "manual"
        };

        // first Cache<T> touch must not throw even though no IIdGenerator exists for int
        Assert.IsFalse(entity.HasDefaultID());
        await _db.SaveAsync(entity);

        var loaded = await _db.Find<NoGeneratorIntEntity>().OneAsync(entity.ID);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("manual", loaded!.Name);

        await _db.DeleteAsync<NoGeneratorIntEntity>(entity.ID);
        Assert.IsNull(await _db.Find<NoGeneratorIntEntity>().OneAsync(entity.ID));
    }

    [TestMethod]
    public async Task missing_generator_throws_only_when_generation_needed()
    {
        var entity = new NoGeneratorIntEntity { Name = "needs-gen" };

        Assert.IsTrue(entity.HasDefaultID());
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _db.SaveAsync(entity));
        StringAssert.Contains(ex.Message, "No IdGenerator could be resolved");
    }

    [TestMethod]
    public async Task entity_level_registration_works_without_type_level_fallback()
    {
        // no BsonSerializer.RegisterIdGenerator for int — only entity-level registration
        DB.RegisterIdGenerator<EntityLevelOnlyIdEntity>(new SequentialIntIdGenerator());

        var entity = new EntityLevelOnlyIdEntity { Name = "entity-level" };
        Assert.IsTrue(entity.HasDefaultID());

        await _db.SaveAsync(entity);

        Assert.AreNotEqual(0, entity.ID);
        var loaded = await _db.Find<EntityLevelOnlyIdEntity>().OneAsync(entity.ID);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("entity-level", loaded!.Name);
    }

    [TestMethod]
    public async Task string_empty_id_is_treated_as_default_and_regenerated()
    {
        var entity = new PlainStringEntity
        {
            Name = "empty-string-id",
            ID = string.Empty
        };

        Assert.IsTrue(entity.HasDefaultID());
        await _db.SaveAsync(entity);

        Assert.IsFalse(string.IsNullOrEmpty(entity.ID));
        Assert.IsTrue(ObjectId.TryParse(entity.ID, out _));
    }

    [TestMethod]
    public async Task custom_isempty_sentinel_is_regenerated_on_save()
    {
        DB.RegisterIdGenerator<SentinelIdEntity>(new SentinelStringIdGenerator());

        var entity = new SentinelIdEntity
        {
            ID = SentinelStringIdGenerator.EmptySentinel,
            Name = "sentinel"
        };

        Assert.IsTrue(entity.HasDefaultID());
        await _db.SaveAsync(entity);

        Assert.AreNotEqual(SentinelStringIdGenerator.EmptySentinel, entity.ID);
        StringAssert.StartsWith(entity.ID, "sentinel-");

        var loaded = await _db.Find<SentinelIdEntity>().OneAsync(entity.ID);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("sentinel", loaded!.Name);
    }

    [TestMethod]
    public async Task custom_isempty_non_empty_value_is_preserved()
    {
        DB.RegisterIdGenerator<SentinelIdEntity>(new SentinelStringIdGenerator());

        var entity = new SentinelIdEntity
        {
            ID = "manual-sentinel-id",
            Name = "kept"
        };

        Assert.IsFalse(entity.HasDefaultID());
        await _db.SaveAsync(entity);
        Assert.AreEqual("manual-sentinel-id", entity.ID);
    }

}

/// <summary>
/// simple sequential int generator for entity-level registration tests
/// </summary>
file class SequentialIntIdGenerator : MongoDB.Bson.Serialization.IIdGenerator
{
    static int _counter = unchecked((int)DateTime.UtcNow.Ticks);

    public object GenerateId(object container, object document)
        => System.Threading.Interlocked.Increment(ref _counter);

    public bool IsEmpty(object id)
        => id is not int value || value == 0;
}
