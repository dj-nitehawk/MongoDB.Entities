using System;
using System.Collections.Generic;
using System.Text;

namespace MongoDB.Entities.ConfigBuilders;

/// <summary>
/// Handles all entity-related config
/// </summary>
public class DBContextConfigBuilder
{
    public DBContextConfigBuilder(DBContext context)
    {
        Context = context;
    }
    internal DBContext Context { get; }
    internal Dictionary<ValueTuple<Type, string>, object> EntityConfigBuilders { get; } = new();
    public EntityConfigBuilder<T> Entity<T>(string? collectionName = null)
    {
        collectionName ??= Context.CollectionName<T>();
        var key = (typeof(T), collectionName);
        if (EntityConfigBuilders.TryGetValue(key, out var config))
        {
            return (EntityConfigBuilder<T>)config;
        }
        var res = new EntityConfigBuilder<T>(this, collectionName);
        EntityConfigBuilders[key] = res;
        return res;
    }
}
