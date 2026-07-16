using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Tests;

/// <summary>
/// Validates the documented Entity-base class-map migration path without registering
/// the real <see cref="Entity"/> type (which would contaminate the main test process).
/// </summary>
[TestClass]
public class EntityClassMap
{
    [TestMethod]
    public void mapping_inherited_id_on_concrete_entity_subclass_throws()
    {
        // The changelog previously recommended RegisterClassMap&lt;Book&gt;(...).MapIdProperty(b => b.ID),
        // which throws because ID is declared on Entity, not Book.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            BsonClassMap.RegisterClassMap<ConcreteEntityBook>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(b => b.ID);
            }));
    }

    [TestMethod]
    public void mapping_id_on_entity_like_base_class_stores_objectid_strings()
    {
        // Mirrors the published guidance: map the ID on the base class that declares it,
        // with StringSerializer(BsonType.ObjectId) + StringObjectIdGenerator.
        // Uses a private hierarchy so the real Entity class map stays untouched.
        if (!BsonClassMap.IsClassMapRegistered(typeof(LegacyIdBase)))
        {
            BsonClassMap.RegisterClassMap<LegacyIdBase>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.MapIdProperty(e => e.ID)
                  .SetSerializer(new StringSerializer(BsonType.ObjectId))
                  .SetIdGenerator(StringObjectIdGenerator.Instance);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(LegacyIdBook)))
        {
            BsonClassMap.RegisterClassMap<LegacyIdBook>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        var map = BsonClassMap.LookupClassMap(typeof(LegacyIdBase));
        Assert.IsNotNull(map.IdMemberMap);
        Assert.IsInstanceOfType(map.IdMemberMap.GetSerializer(), typeof(StringSerializer));
        Assert.IsInstanceOfType(map.IdMemberMap.IdGenerator, typeof(StringObjectIdGenerator));

        var id = ObjectId.GenerateNewId();
        var doc = new BsonDocument
        {
            { "_id", id },
            { "Title", "legacy-book" }
        };

        var book = BsonSerializer.Deserialize<LegacyIdBook>(doc);
        Assert.AreEqual(id.ToString(), book.ID);
        Assert.AreEqual("legacy-book", book.Title);

        var written = book.ToBsonDocument();
        Assert.AreEqual(BsonType.ObjectId, written["_id"].BsonType);
        Assert.AreEqual(id, written["_id"].AsObjectId);

        // Per-entity representation still cannot remap the inherited base member.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            BsonClassMap.RegisterClassMap<LegacyIdBook>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(b => b.ID);
            }));
    }

    class ConcreteEntityBook : Entity
    {
        public string Title { get; set; } = null!;
    }

    abstract class LegacyIdBase
    {
        [BsonId]
        public string ID { get; set; } = null!;
    }

    class LegacyIdBook : LegacyIdBase
    {
        public string Title { get; set; } = null!;
    }
}
