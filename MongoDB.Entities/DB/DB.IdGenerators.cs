using MongoDB.Bson.Serialization;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Registers an IIdGenerator for generating new ID values for a single entity type. This works for any entity —
    /// including ones inheriting the ID property from a base class such as <see cref="MongoDB.Entities.Entity" /> — and takes precedence
    /// over generators configured on the entity's BsonClassMap as well as generators registered for the ID's CLR type.
    /// <para>TIP: To register a generator for ALL entities with a given ID type, use BsonSerializer.RegisterIdGenerator() instead.</para>
    /// </summary>
    /// <typeparam name="TEntity">The entity type the generator will be used for</typeparam>
    /// <param name="generator">The IIdGenerator instance that will supply new ID values</param>
    public static void RegisterIdGenerator<TEntity>(IIdGenerator generator) where TEntity : IEntity
        => Cache<TEntity>.SetIdGenerator(generator);
}
