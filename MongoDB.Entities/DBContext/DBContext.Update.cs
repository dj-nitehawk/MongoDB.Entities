namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Starts an update command for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public Update<T, TId> Update<T, TId>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        var cmd = new Update<T, TId>(this, Collection(collectionName, collection), OnBeforeUpdate<T, TId, Update<T, TId>>);
        if (Cache<T>().ModifiedByProp is PropertyInfo ModifiedByProp)
        {
            ThrowIfModifiedByIsEmpty<T>();
            cmd.Modify(b => b.Set(ModifiedByProp.Name, ModifiedBy));
        }
        return cmd;
    }

    /// <summary>
    /// Starts an update-and-get command for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public UpdateAndGet<T, TId> UpdateAndGet<T, TId>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return UpdateAndGet<T, TId>(collectionName, collection);
    }

    /// <summary>
    /// Starts an update-and-get command with projection support for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TProjection">The type of the end result</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public UpdateAndGet<T, TId, TProjection> UpdateAndGet<T, TId, TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null)
         where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        var cmd = new UpdateAndGet<T, TId, TProjection>(this, Collection(collectionName, collection), OnBeforeUpdate<T, TId, UpdateAndGet<T, TId, TProjection>>);
        if (Cache<T>().ModifiedByProp is PropertyInfo ModifiedByProp)
        {
            ThrowIfModifiedByIsEmpty<T>();
            cmd.Modify(b => b.Set(ModifiedByProp.Name, ModifiedBy));
        }
        return cmd;
    }
}
