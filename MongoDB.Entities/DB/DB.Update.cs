namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Starts an update command for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public Update<T> Update<T>() where T : IEntity
    {
        var cmd = new Update<T>(SessionHandle, _globalFilters, OnBeforeUpdate<T>(), this);

        if (Cache<T>.ModifiedByProp == null)
            return cmd;

        ThrowIfModifiedByIsEmpty<T>();
        cmd.Modify(b => b.Set(Cache<T>.ModifiedByProp.Name, ModifiedBy));

        return cmd;
    }

    /// <summary>
    /// Starts an update-and-get command for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public UpdateAndGet<T, T> UpdateAndGet<T>() where T : IEntity
        => UpdateAndGet<T, T>();

    /// <summary>
    /// Starts an update-and-get command with projection support for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TProjection">The type of the end result</typeparam>
    public UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
    {
        var cmd = new UpdateAndGet<T, TProjection>(SessionHandle, _globalFilters, OnBeforeUpdate<T>(), this);

        if (Cache<T>.ModifiedByProp == null)
            return cmd;

        ThrowIfModifiedByIsEmpty<T>();
        cmd.Modify(b => b.Set(Cache<T>.ModifiedByProp.Name, ModifiedBy));

        return cmd;
    }
}