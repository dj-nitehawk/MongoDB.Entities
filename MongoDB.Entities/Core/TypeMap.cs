using System;
using System.Collections.Concurrent;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class TypeMap
{
    static readonly ConcurrentDictionary<Type, DBInstance> _typeToDbInstanceMap = new();
    static readonly ConcurrentDictionary<Type, string> _typeToCollMap = new();

    internal static void AddCollectionMapping(Type entityType, string collectionName)
        => _typeToCollMap[entityType] = collectionName;

    internal static string? GetCollectionName(Type entityType)
    {
        _typeToCollMap.TryGetValue(entityType, out var name);

        return name;
    }

    internal static void AddDbInstanceMapping(Type entityType, DBInstance dbInstance)
        => _typeToDbInstanceMap[entityType] = dbInstance;

    internal static void Clear()
    {
        _typeToDbInstanceMap.Clear();
        _typeToCollMap.Clear();
    }

    internal static DBInstance GetDbInstance(Type entityType)
    {
        _typeToDbInstanceMap.TryGetValue(entityType, out var dbInstance);

        return dbInstance ?? DB.DbInstance() ?? throw new InvalidOperationException("DB not initialized. Call DB.InitAsync(...) first!");
    }
}