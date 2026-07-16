using System;
using MongoDB.Bson;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the name of the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static string GetIdName<T>(this T entity) where T : IEntity
        => Cache<T>.IdPropName;

    /// <summary>
    /// Gets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static object GetId<T>(this T entity) where T : IEntity
        => Cache<T>.IdGetter(entity);

    /// <summary>
    /// Gets stored representation of the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static BsonValue GetBsonId<T>(this T entity) where T : IEntity
        => Cache<T>.IdToBsonValue(entity.GetId());

    /// <summary>
    /// Sets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static void SetId<T>(this T entity, object id) where T : IEntity
        => Cache<T>.IdSetter(entity, id);

    /// <summary>
    /// Determines whether the entity's ID is considered empty/unset (i.e. the entity hasn't been saved yet
    /// and needs a new ID generated on save). When an <see cref="MongoDB.Bson.Serialization.IIdGenerator"/> is
    /// resolved for the entity, that generator's <c>IsEmpty</c> is used; otherwise the ID is compared to the
    /// default value of its CLR type.
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    public static bool HasDefaultID<T>(this T entity) where T : IEntity
        => Cache<T>.IdGenerator is { } generator
               ? generator.IsEmpty(Cache<T>.IdGetter(entity))
               : Equals(Cache<T>.IdGetter(entity), Cache<T>.IdDefaultValue);

    /// <summary>
    /// Generates a new ID value for the entity using the IIdGenerator resolved for the entity's ID property.
    /// Resolution order: a generator registered with <c>DB.RegisterIdGenerator&lt;T&gt;()</c> (highest precedence,
    /// order-independent), then the entity's BsonClassMap IdGenerator (settable via
    /// <c>cm.IdMemberMap.SetIdGenerator()</c>), then a generator registered with
    /// <c>BsonSerializer.RegisterIdGenerator()</c> for the ID's CLR type, and finally the library defaults
    /// for string (ObjectId format), ObjectId and Guid IDs.
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    /// <exception cref="InvalidOperationException">thrown when no IIdGenerator could be resolved for the ID property</exception>
    public static object GenerateNewID<T>(this T entity) where T : IEntity
        => Cache<T>.IdGenerator is { } generator
               ? generator.GenerateId(null, entity)
               : throw new InvalidOperationException(
                     $"No IdGenerator could be resolved for the ID property of '{typeof(T).Name}'. Either register one via " +
                     "DB.RegisterIdGenerator()/BsonSerializer.RegisterIdGenerator()/BsonClassMap SetIdGenerator(), or set the ID value manually before saving.");
}