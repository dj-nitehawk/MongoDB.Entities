using MongoDB.Driver;
using System;
using System.Collections.Concurrent;

namespace MongoDB.Entities
{
    internal static class TypeMap
    {
        //key: TenantPrefix_EntityTypeName
        //val: IMongoDatabase instance
        //private static readonly ConcurrentDictionary<string, IMongoDatabase> TypeToDBMap = new();

        //key: TenantPrefix_EntityTypeName
        //val: Collection Name (ex: Author, Book)
        //private static readonly ConcurrentDictionary<string, string> TypeToCollMap = new();

        //internal static void AddCollectionMapping(Type entityType, string collectionName)
        //    => TypeToCollMap[entityType] = collectionName;

        //internal static string GetCollectionName(Type entityType)
        //{
        //    TypeToCollMap.TryGetValue(entityType, out string name);
        //    return name;
        //}

        //internal static void AddDatabaseMapping(Type entityType, IMongoDatabase database)
        //    => TypeToDBMap[entityType] = database;

        //internal static void Clear()
        //{
        //    TypeToDBMap.Clear();
        //    TypeToCollMap.Clear();
        //}

        //internal static IMongoDatabase GetDatabase(Type entityType)
        //{
        //    TypeToDBMap.TryGetValue(entityType, out IMongoDatabase db);
        //    return db ?? DB.Database(default);
        //}
    }
}