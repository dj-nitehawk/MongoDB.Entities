using MongoDB.Driver;
using System;
using System.Collections.Concurrent;

namespace MongoDB.Entities;

static class TypeMap
{
    static readonly ConcurrentDictionary<Type, IMongoDatabase> TypeToDBMap = new();
    static readonly ConcurrentDictionary<Type, string> TypeToCollMap = new();

    internal static void AddCollectionMapping(Type entityType, string collectionName)
        => TypeToCollMap[entityType] = collectionName;

    internal static string? GetCollectionName(Type entityType)
    {
        TypeToCollMap.TryGetValue(entityType, out var name);
        return name;
    }

    internal static void AddDatabaseMapping(Type entityType, IMongoDatabase database)
        => TypeToDBMap[entityType] = database;

    internal static void Clear()
    {
        TypeToDBMap.Clear();
        TypeToCollMap.Clear();
    }

    internal static IMongoDatabase GetDatabase(Type entityType)
    {
        TypeToDBMap.TryGetValue(entityType, out var db);
        return db ?? DB.Database(default);
    }
}