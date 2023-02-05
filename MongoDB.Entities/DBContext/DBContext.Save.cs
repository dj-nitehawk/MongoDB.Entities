using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="entity">The instance to persist</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public Task SaveAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);
        return DB.SaveAsync(entity, Session, cancellation);
    }

    /// <summary>
    /// Saves a batch of complete entities replacing an existing entities or creating a new ones if they do not exist. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="entities">The entities to persist</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedByMultiple(entities);
        foreach (var ent in entities) OnBeforeSave<T>()?.Invoke(ent);
        return DB.SaveAsync(entities, Session, cancellation);
    }

    /// <summary>
    /// Saves an entity partially with only the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
    /// You can only specify root level properties with the expression.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object?>> members, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);
        return DB.SaveOnlyAsync(entity, members, Session, cancellation);
    }

    /// <summary>
    /// Saves an entity partially with only the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be saved can be specified with an IEnumerable. 
    /// Property names must match exactly.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveOnlyAsync<T>(T entity, IEnumerable<string> propNames, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);
        return DB.SaveOnlyAsync(entity, propNames, Session, cancellation);
    }

    /// <summary>
    /// Saves a batch of entities partially with only the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
    /// You can only specify root level properties with the expression.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, Expression<Func<T, object?>> members, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedByMultiple(entities);
        foreach (var ent in entities) OnBeforeSave<T>()?.Invoke(ent);
        return DB.SaveOnlyAsync(entities, members, Session, cancellation);
    }

    /// <summary>
    /// Saves a batch of entities partially with only the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be saved can be specified with an IEnumerable. 
    /// Property names must match exactly.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, IEnumerable<string> propNames, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedByMultiple(entities);
        foreach (var ent in entities) OnBeforeSave<T>()?.Invoke(ent);
        return DB.SaveOnlyAsync(entities, propNames, Session, cancellation);
    }

    /// <summary>
    /// Saves an entity partially excluding the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
    /// You can only specify root level properties with the expression.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object?>> members, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);
        return DB.SaveExceptAsync(entity, members, Session, cancellation);
    }

    /// <summary>
    /// Saves an entity partially excluding the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be saved can be specified with an IEnumerable. 
    /// Property names must match exactly.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveExceptAsync<T>(T entity, IEnumerable<string> propNames, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);
        return DB.SaveExceptAsync(entity, propNames, Session, cancellation);
    }

    /// <summary>
    /// Saves a batch of entities partially excluding the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
    /// You can only specify root level properties with the expression.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, Expression<Func<T, object?>> members, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedByMultiple(entities);
        foreach (var ent in entities) OnBeforeSave<T>()?.Invoke(ent);
        return DB.SaveExceptAsync(entities, members, Session, cancellation);
    }

    /// <summary>
    /// Saves a batch of entities partially excluding the specified subset of properties. 
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>TIP: The properties to be saved can be specified with an IEnumerable. 
    /// Property names must match exactly.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, IEnumerable<string> propNames, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedByMultiple(entities);
        foreach (var ent in entities) OnBeforeSave<T>()?.Invoke(ent);
        return DB.SaveExceptAsync(entities, propNames, Session, cancellation);
    }

    /// <summary>
    /// Saves an entity partially while excluding some properties
    /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SavePreservingAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);
        return DB.SavePreservingAsync(entity, Session, cancellation);
    }

    private void SetModifiedBySingle<T>(T entity) where T : IEntity
    {
        ThrowIfModifiedByIsEmpty<T>();
        Cache<T>.ModifiedByProp?.SetValue(
            entity,
            BsonSerializer.Deserialize(ModifiedBy.ToBson(), Cache<T>.ModifiedByProp.PropertyType));
        //note: we can't use an IModifiedBy interface because the above line needs a concrete type
        //      to be able to correctly deserialize a user supplied derived/sub class of ModifiedOn.
    }

    private void SetModifiedByMultiple<T>(IEnumerable<T> entities) where T : IEntity
    {
        if (Cache<T>.ModifiedByProp is null)
            return;

        ThrowIfModifiedByIsEmpty<T>();

        var val = BsonSerializer.Deserialize(ModifiedBy.ToBson(), Cache<T>.ModifiedByProp.PropertyType);

        foreach (var e in entities)
            Cache<T>.ModifiedByProp.SetValue(e, val);
    }
}
