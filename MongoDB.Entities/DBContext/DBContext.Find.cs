using MongoDB.Driver;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Starts a find command for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public Find<T, TId> Find<T, TId>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return new Find<T, TId>(this, Collection(collectionName, collection));
    }

    /// <summary>
    /// Starts a find command with projection support for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <typeparam name="TProjection">The type of the end result</typeparam>
    public Find<T, TId, TProjection> Find<T, TId, TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return new Find<T, TId, TProjection>(this, Collection(collectionName, collection));
    }
}
