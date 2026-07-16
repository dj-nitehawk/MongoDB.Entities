using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Tests;

[TestClass]
public class SerializerRegistration
{
    [TestMethod]
    public void user_registered_decimal_serializer_wins_over_library_default()
    {
        // library defaults are provided lazily via IBsonSerializationProvider. a direct
        // RegisterSerializer for the same type must succeed (and stick) when issued before
        // the next lookup — unlike TryRegisterSerializer, which throws if a different
        // serializer is already registered.
        RemoveCachedSerializer(typeof(decimal));

        var userSerializer = new DecimalSerializer(BsonType.String);
        BsonSerializer.RegisterSerializer(typeof(decimal), userSerializer);

        var resolved = BsonSerializer.LookupSerializer(typeof(decimal));
        Assert.AreSame(userSerializer, resolved);
        Assert.AreEqual(BsonType.String, ((DecimalSerializer)resolved).Representation);

        // restore library default for other tests that expect Decimal128
        RemoveCachedSerializer(typeof(decimal));
        var restored = (DecimalSerializer)BsonSerializer.LookupSerializer(typeof(decimal));
        Assert.AreEqual(BsonType.Decimal128, restored.Representation);
    }

    [TestMethod]
    public void library_provider_supplies_date_and_fuzzystring_serializers()
    {
        RemoveCachedSerializer(typeof(Date));
        RemoveCachedSerializer(typeof(FuzzyString));

        var date = BsonSerializer.LookupSerializer(typeof(Date));
        var fuzzy = BsonSerializer.LookupSerializer(typeof(FuzzyString));

        Assert.IsNotNull(date);
        Assert.IsNotNull(fuzzy);
        Assert.AreEqual(typeof(Date), date.ValueType);
        Assert.AreEqual(typeof(FuzzyString), fuzzy.ValueType);
    }

    static void RemoveCachedSerializer(Type type)
    {
        var registryField = typeof(BsonSerializer).GetField("__serializerRegistry", BindingFlags.NonPublic | BindingFlags.Static)
                            ?? throw new InvalidOperationException("BsonSerializer.__serializerRegistry not found");
        var registry = registryField.GetValue(null)
                       ?? throw new InvalidOperationException("serializer registry is null");
        var cacheField = registry.GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new InvalidOperationException("serializer registry _cache not found");
        var cache = (ConcurrentDictionary<Type, IBsonSerializer>)(cacheField.GetValue(registry)
                                                                  ?? throw new InvalidOperationException("serializer cache is null"));
        cache.TryRemove(type, out _);
    }
}
