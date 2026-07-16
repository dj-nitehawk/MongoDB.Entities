using System;
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
}

/// <summary>
/// simple sequential int generator for entity-level registration tests
/// </summary>
file class SequentialIntIdGenerator : MongoDB.Bson.Serialization.IIdGenerator
{
    static int _counter;

    public object GenerateId(object container, object document)
        => System.Threading.Interlocked.Increment(ref _counter);

    public bool IsEmpty(object id)
        => id is not int value || value == 0;
}
