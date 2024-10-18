using System;
using System.Collections.Concurrent;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class TypeMap
{
    static readonly ConcurrentDictionary<Type, IMongoDatabase> _typeToDbMap = new();
    static readonly ConcurrentDictionary<Type, string> _typeToCollMap = new();

    internal static void AddCollectionMapping(Type entityType, string collectionName)
        => _typeToCollMap[entityType] = collectionName;

    internal static string? GetCollectionName(Type entityType)
    {
        _typeToCollMap.TryGetValue(entityType, out var name);

        return name;
    }

    internal static void AddDatabaseMapping(Type entityType, IMongoDatabase database)
        => _typeToDbMap[entityType] = database;

    internal static void Clear()
    {
        _typeToDbMap.Clear();
        _typeToCollMap.Clear();
    }

    internal static IMongoDatabase GetDatabase(Type entityType)
    {
        _typeToDbMap.TryGetValue(entityType, out var db);

        return db ?? DB.Database(default);
    }
}