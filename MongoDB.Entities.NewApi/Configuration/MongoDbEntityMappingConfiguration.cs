namespace MongoDB.Entities.Configuration;

using System;

/// <summary>
/// Stores mapping configuration for multiple entities
/// </summary>
public class MongoDbEntityMappingConfiguration
{
    public MongoDbEntityMappingConfiguration ConfigEntity<T>(Action<EntityConfigBuilder<T>> action)
    {
        action(new(this));
        return this;
    }
    internal Dictionary<Type, object> PerTypeConfig { get; } = new();

    internal PerTypeConfiguration<T>? GetConfigOfType<T>(Func<PerTypeConfiguration<T>>? orDefault = null)
    {
        var t = typeof(T);
        if (PerTypeConfig.TryGetValue(t, out var res))
        {
            return (PerTypeConfiguration<T>)res;
        }
        if (orDefault != null)
        {
            PerTypeConfig[t] = orDefault();
        }
        return null;
    }
}
