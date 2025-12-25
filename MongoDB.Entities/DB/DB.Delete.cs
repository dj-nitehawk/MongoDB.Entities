using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    const int DeleteBatchSize = 100000;

    // ReSharper disable once InconsistentNaming
    async Task<DeleteResult> DeleteCascadingAsync<T>(IEnumerable<object?> IDs, CancellationToken cancellation) where T : IEntity
    {
        // note: cancellation should not be enabled outside of transactions because multiple collections are involved 
        //       and premature cancellation could cause data inconsistencies.
        //       i.e. don't pass the cancellation token to delete methods below that don't take a session.
        //       also make consumers call ThrowIfCancellationNotSupported() before calling this method.

        ThrowIfCancellationNotSupported(SessionHandle, cancellation);

        var tasks = new List<Task>();

        foreach (var refCollection in Cache<T>.ReferenceCollections.Values)
        {
            // ReSharper disable once MethodSupportsCancellation
            tasks.Add(
                SessionHandle == null
                    ? refCollection.DeleteManyAsync(r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID))
                    : refCollection.DeleteManyAsync(SessionHandle, r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID), null, cancellation));
        }

        var filter = Logic.MergeWithGlobalFilter(IgnoreGlobalFilters, _globalFilters, Builders<T>.Filter.In(Cache<T>.IdPropName, IDs));

        // ReSharper disable once MethodSupportsCancellation
        var delResTask = SessionHandle == null
                             ? Collection<T>().DeleteManyAsync(filter)
                             : Collection<T>().DeleteManyAsync(SessionHandle, filter, null, cancellation);

        tasks.Add(delResTask);

        var baseType = typeof(T).BaseType;

        if (baseType is { IsGenericType: true } && baseType.GetGenericTypeDefinition() == typeof(FileEntity<>))
        {
            // ReSharper disable once MethodSupportsCancellation
            tasks.Add(
                SessionHandle == null
                    ? _mongoDb.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(x => IDs.Contains(x.FileID))
                    : _mongoDb.GetCollection<FileChunk>(CollectionName<FileChunk>())
                              .DeleteManyAsync(SessionHandle, x => IDs.Contains(x.FileID), null, cancellation));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return await delResTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a single entity from MongoDB.
    /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to delete</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<DeleteResult> DeleteAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        => DeleteCascadingAsync<T>([entity.GetId()], cancellation);

    /// <summary>
    /// Deletes a single entity from MongoDB.
    /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="ID">The Id of the entity to delete</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<DeleteResult> DeleteAsync<T>(object ID, CancellationToken cancellation = default) where T : IEntity
        => DeleteCascadingAsync<T>([ID], cancellation);

    /// <summary>
    /// Deletes entities using a collection of IDs
    /// <para>HINT: If more than 100,000 IDs are passed in, they will be processed in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">An IEnumerable of entities</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<DeleteResult> DeleteAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        => DeleteAsync<T>(entities.Select(e => e.GetId()), cancellation);

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Deletes entities using a collection of IDs
    /// <para>HINT: If more than 100,000 IDs are passed in, they will be processed in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="IDs">An IEnumerable of entity IDs</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<DeleteResult> DeleteAsync<T>(IEnumerable<object?> IDs, CancellationToken cancellation = default) where T : IEntity
    {
        if (IDs.Count() <= DeleteBatchSize)
            return await DeleteCascadingAsync<T>(IDs, cancellation).ConfigureAwait(false);

        long deletedCount = 0;
        DeleteResult res = DeleteResult.Unacknowledged.Instance;

        foreach (var batch in IDs.ToBatches(DeleteBatchSize))
        {
            res = await DeleteCascadingAsync<T>(batch, cancellation).ConfigureAwait(false);
            deletedCount += res.DeletedCount;
        }

        if (res.IsAcknowledged)
            res = new DeleteResult.Acknowledged(deletedCount);

        return res;
    }

    /// <summary>
    /// Deletes matching entities with an expression
    /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="expression">A lambda expression for matching entities to delete.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="collation">An optional collation object</param>
    public Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default, Collation? collation = null)
        where T : IEntity
        => DeleteAsync(Builders<T>.Filter.Where(expression), cancellation, collation);

    /// <summary>
    /// Deletes matching entities with a filter expression
    /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="collation">An optional collation object</param>
    public Task<DeleteResult> DeleteAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter,
                                             CancellationToken cancellation = default,
                                             Collation? collation = null) where T : IEntity
        => DeleteAsync(filter(Builders<T>.Filter), cancellation, collation);

    /// <summary>
    /// Deletes matching entities with a filter definition
    /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="filter">A filter definition for matching entities to delete.</param>
    /// <param name="collation">An optional collation object</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<DeleteResult> DeleteAsync<T>(FilterDefinition<T> filter, CancellationToken cancellation = default, Collation? collation = null)
        where T : IEntity
    {
        ThrowIfCancellationNotSupported(SessionHandle, cancellation);

        //workaround for the newly added implicit operator in driver which matches all strings as json filters
        var jsonFilter = filter as JsonFilterDefinition<T>;
        if (jsonFilter?.Json.StartsWith("{") is false)
            filter = Builders<T>.Filter.Eq(Cache<T>.IdExpression, jsonFilter.Json);

        var cursor = await new Find<T, object>(SessionHandle, null, this)
                           .Match(_ => filter)
                           .Project(p => p.Include(Cache<T>.IdPropName))
                           .Option(o => o.BatchSize = DeleteBatchSize)
                           .Option(o => o.Collation = collation)
                           .ExecuteCursorAsync(cancellation)
                           .ConfigureAwait(false);

        long deletedCount = 0;
        DeleteResult res = DeleteResult.Unacknowledged.Instance;

        using (cursor)
        {
            while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                if (!cursor.Current.Any())
                    continue;

                var idObjects = ValidateCursor((List<object>)cursor.Current);
                res = await DeleteCascadingAsync<T>(idObjects, cancellation).ConfigureAwait(false);
                deletedCount += res.DeletedCount;
            }
        }

        if (res.IsAcknowledged)
            res = new DeleteResult.Acknowledged(deletedCount);

        return res;
    }

    static IEnumerable<object> ValidateCursor(IReadOnlyList<object> idObjects)
    {
        if (!idObjects.Any() || idObjects[0] is not ExpandoObject)
            return idObjects;

        List<object> ids = [];

        for (var i = 0; i < idObjects.Count; i++)
        {
            var item = (IDictionary<string, object>)idObjects[i];
            ids.Add(item["_id"]);
        }

        return ids;
    }

    static void ThrowIfCancellationNotSupported(IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        if (cancellation != CancellationToken.None && session == null)
            throw new NotSupportedException("Cancellation is only supported within transactions for delete operations!");
    }
}