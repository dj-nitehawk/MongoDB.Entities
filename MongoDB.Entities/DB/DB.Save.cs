using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    readonly BulkWriteOptions _unOrdBlkOpts = new() { IsOrdered = false };
    readonly UpdateOptions _updateOptions = new() { IsUpsert = true };

    /// <summary>
    /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The instance to persist</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public Task SaveAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
    {
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);

        var filter = Builders<T>.Filter.Eq(Cache<T>.IdPropName, entity.GetId());

        return PrepAndCheckIfInsert(entity)
                   ? Session == null
                         ? Collection<T>().InsertOneAsync(entity, null, cancellation)
                         : Collection<T>().InsertOneAsync(Session, entity, null, cancellation)
                   : Session == null
                       ? Collection<T>().ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, cancellation)
                       : Collection<T>().ReplaceOneAsync(Session, filter, entity, new ReplaceOptions { IsUpsert = true }, cancellation);
    }

    /// <summary>
    /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The entities to persist</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
    {
        var models = new List<WriteModel<T>>(entities.Count());

        foreach (var ent in entities)
        {
            if (PrepAndCheckIfInsert(ent))
                models.Add(new InsertOneModel<T>(ent));
            else
            {
                models.Add(
                    new ReplaceOneModel<T>(
                            filter: Builders<T>.Filter.Eq(ent.GetIdName(), ent.GetId()),
                            replacement: ent)
                        { IsUpsert = true });
            }
            SetModifiedBySingle(ent);
            OnBeforeSave<T>()?.Invoke(ent);
        }

        return Session == null
                   ? Collection<T>().BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : Collection<T>().BulkWriteAsync(Session, models, _unOrdBlkOpts, cancellation);
    }

    /// <summary>
    /// Saves an entity partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object?>> members, CancellationToken cancellation = default) where T : IEntity
        => SavePartial(entity, Logic.GetPropNamesFromExpression(members), cancellation);

    /// <summary>
    /// Saves an entity partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveOnlyAsync<T>(T entity, IEnumerable<string> propNames, CancellationToken cancellation = default) where T : IEntity
        => SavePartial(entity, propNames, cancellation);

    /// <summary>
    /// Saves a batch of entities partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, Expression<Func<T, object?>> members, CancellationToken cancellation = default)
        where T : IEntity
        => SavePartial(entities, Logic.GetPropNamesFromExpression(members), cancellation);

    /// <summary>
    /// Saves a batch of entities partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, IEnumerable<string> propNames, CancellationToken cancellation = default)
        where T : IEntity
        => SavePartial(entities, propNames, cancellation);

    /// <summary>
    /// Saves an entity partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be excluded can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object?>> members, CancellationToken cancellation = default) where T : IEntity
        => SavePartial(entity, Logic.GetPropNamesFromExpression(members), cancellation, true);

    /// <summary>
    /// Saves an entity partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SaveExceptAsync<T>(T entity, IEnumerable<string> propNames, CancellationToken cancellation = default) where T : IEntity
        => SavePartial(entity, propNames, cancellation, true);

    /// <summary>
    /// Saves a batch of entities partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be excluded can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, Expression<Func<T, object?>> members, CancellationToken cancellation = default)
        where T : IEntity
        => SavePartial(entities, Logic.GetPropNamesFromExpression(members), cancellation, true);

    /// <summary>
    /// Saves a batch of entities partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, IEnumerable<string> propNames, CancellationToken cancellation = default)
        where T : IEntity
        => SavePartial(entities, propNames, cancellation, true);

    /// <summary>
    /// Saves an entity partially while excluding some properties.
    /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> SavePreservingAsync<T>(T entity, CancellationToken cancellation = default)
        where T : IEntity
    {
        entity.ThrowIfUnsaved();

        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);

        var propsToUpdate = Cache<T>.UpdatableProps(entity);

        IEnumerable<string> propsToPreserve = Array.Empty<string>();

        var dontProps = propsToUpdate.Where(p => p.IsDefined(typeof(DontPreserveAttribute), false)).Select(p => p.Name);
        var presProps = propsToUpdate.Where(p => p.IsDefined(typeof(PreserveAttribute), false)).Select(p => p.Name);

        if (dontProps.Any() && presProps.Any())
            throw new NotSupportedException("[Preserve] and [DontPreserve] attributes cannot be used together on the same entity!");

        if (dontProps.Any())
            propsToPreserve = propsToUpdate.Where(p => !dontProps.Contains(p.Name)).Select(p => p.Name);

        if (presProps.Any())
            propsToPreserve = propsToUpdate.Where(p => presProps.Contains(p.Name)).Select(p => p.Name);

        if (!propsToPreserve.Any())
            throw new ArgumentException("No properties are being preserved. Please use .SaveAsync() method instead!");

        propsToUpdate = propsToUpdate.Where(p => !propsToPreserve.Contains(p.Name));

        var propsToUpdateCount = propsToUpdate.Count();

        if (propsToUpdateCount == 0)
            throw new ArgumentException("At least one property must be not preserved!");

        var defs = new List<UpdateDefinition<T>>(propsToUpdateCount);
        defs.AddRange(
            propsToUpdate.Select(
                p => p.Name == Cache<T>.ModifiedOnPropName
                         ? Builders<T>.Update.CurrentDate(Cache<T>.ModifiedOnPropName)
                         : Builders<T>.Update.Set(p.Name, p.GetValue(entity))));

        var filter = Builders<T>.Filter.Eq(entity.GetIdName(), entity.GetId());

        return
            Session == null
                ? Collection<T>().UpdateOneAsync(filter, Builders<T>.Update.Combine(defs), _updateOptions, cancellation)
                : Collection<T>().UpdateOneAsync(Session, filter, Builders<T>.Update.Combine(defs), _updateOptions, cancellation);
    }

    Task<UpdateResult> SavePartial<T>(T entity, IEnumerable<string> propNames, CancellationToken cancellation, bool excludeMode = false) where T : IEntity
    {
        PrepAndCheckIfInsert(entity); //just prep. we don't care about inserts here
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);

        var filter = Builders<T>.Filter.Eq(entity.GetIdName(), entity.GetId());

        return
            Session == null
                ? Collection<T>().UpdateOneAsync(
                    filter,
                    Builders<T>.Update.Combine(Logic.BuildUpdateDefs(entity, propNames, excludeMode)),
                    _updateOptions,
                    cancellation)
                : Collection<T>().UpdateOneAsync(
                    Session,
                    filter,
                    Builders<T>.Update.Combine(Logic.BuildUpdateDefs(entity, propNames, excludeMode)),
                    _updateOptions,
                    cancellation);
    }

    Task<BulkWriteResult<T>> SavePartial<T>(IEnumerable<T> entities, IEnumerable<string> propNames, CancellationToken cancellation, bool excludeMode = false)
        where T : IEntity
    {
        var models = new List<WriteModel<T>>(entities.Count());

        foreach (var ent in entities)
        {
            PrepAndCheckIfInsert(ent); //just prep. we don't care about inserts here
            SetModifiedBySingle(ent);
            OnBeforeSave<T>()?.Invoke(ent);
            models.Add(
                new UpdateOneModel<T>(
                        filter: Builders<T>.Filter.Eq(ent.GetIdName(), ent.GetId()),
                        update: Builders<T>.Update.Combine(Logic.BuildUpdateDefs(ent, propNames, excludeMode)))
                    { IsUpsert = true });
        }

        return Session == null
                   ? Collection<T>().BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : Collection<T>().BulkWriteAsync(Session, models, _unOrdBlkOpts, cancellation);
    }

    static bool PrepAndCheckIfInsert<T>(T entity) where T : IEntity
    {
        if (entity.HasDefaultID())
        {
            entity.SetId(entity.GenerateNewID());
            if (Cache<T>.HasCreatedOn)
                ((ICreatedOn)entity).CreatedOn = DateTime.UtcNow;
            if (Cache<T>.HasModifiedOn)
                ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;

            return true;
        }

        if (Cache<T>.HasModifiedOn)
            ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;

        return false;
    }

    void SetModifiedBySingle<T>(T entity) where T : IEntity
    {
        ThrowIfModifiedByIsEmpty<T>();
        Cache<T>.ModifiedByProp?.SetValue(entity, BsonSerializer.Deserialize(ModifiedBy.ToBson(), Cache<T>.ModifiedByProp.PropertyType));

        //note: we can't use an IModifiedBy interface because the above line needs a concrete type
        //      to be able to correctly deserialize a user supplied derived/subclass of ModifiedOn.
    }

    // void SetModifiedByMultiple<T>(IEnumerable<T> entities) where T : IEntity
    // {
    //     if (Cache<T>.ModifiedByProp is null)
    //         return;
    //
    //     ThrowIfModifiedByIsEmpty<T>();
    //
    //     var val = BsonSerializer.Deserialize(ModifiedBy.ToBson(), Cache<T>.ModifiedByProp.PropertyType);
    //
    //     foreach (var e in entities)
    //         Cache<T>.ModifiedByProp.SetValue(e, val);
    // }

    void ThrowIfModifiedByIsEmpty<T>() where T : IEntity
    {
        if (Cache<T>.ModifiedByProp != null && ModifiedBy is null)
        {
            throw new InvalidOperationException(
                $"A value for [{Cache<T>.ModifiedByProp.Name}] must be specified when saving/updating entities of type [{Cache<T>.CollectionName}]");
        }
    }
}