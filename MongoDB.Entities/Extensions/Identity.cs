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
    {
        var bsonEntity = entity.ToBsonDocument();

        return bsonEntity.GetValue(Cache<T>.IdBsonName);
    }

    /// <summary>
    /// Sets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static void SetId<T>(this T entity, object id) where T : IEntity
        => Cache<T>.IdSetter(entity, id);

    /// <summary>
    /// Determines whether the entity's ID property still holds the default value of its type
    /// (i.e. the entity hasn't been saved yet and needs a new ID generated on save).
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    public static bool HasDefaultID<T>(this T entity) where T : IEntity
        => Equals(Cache<T>.IdGetter(entity), Cache<T>.IdDefaultValue);
}