using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public partial class DBContext
{
    private static readonly int _deleteBatchSize = 100000;
    private void ThrowIfCancellationNotSupported(CancellationToken cancellation = default)
    {
        if (cancellation != default && Session is null)
            throw new NotSupportedException("Cancellation is only supported within transactions for delete operations!");
    }

    private async Task<DeleteResult> DeleteCascadingAsync<T, TId>(IEnumerable<TId?> IDs, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        // note: cancellation should not be enabled outside of transactions because multiple collections are involved 
        //       and premature cancellation could cause data inconsistencies.
        //       i.e. don't pass the cancellation token to delete methods below that don't take a session.
        //       also make consumers call ThrowIfCancellationNotSupported() before calling this method.

        var options = new ListCollectionNamesOptions
        {
            Filter = "{$and:[{name:/~/},{name:/" + collectionName ?? Cache<T>().CollectionName + "/}]}"
        };

        var tasks = new List<Task>();

        // note: db.listCollections() mongo command does not support transactions.
        //       so don't add session support here.
        var collNamesCursor = await ListCollectionNamesAsync(options, cancellation).ConfigureAwait(false);
        IDs = IDs.OfType<TId>();
        var casted = IDs.Cast<object>();
        foreach (var cName in await collNamesCursor.ToListAsync(cancellation).ConfigureAwait(false))
        {
            tasks.Add(
                Session is null
                ? Collection<JoinRecord<object, object>>(cName).DeleteManyAsync(r => casted.Contains(r.ID.ChildID) || casted.Contains(r.ID.ParentID))
                : Collection<JoinRecord<object, object>>(cName).DeleteManyAsync(Session, r => casted.Contains(r.ID.ChildID) || casted.Contains(r.ID.ParentID), null, cancellation));
        }

        var delResTask =
                Session == null
                ? Collection(collectionName, collection).DeleteManyAsync(x => IDs.Contains(x.ID))
                : Collection(collectionName, collection).DeleteManyAsync(Session, x => IDs.Contains(x.ID), null, cancellation);

        tasks.Add(delResTask);

        if (typeof(FileEntity).IsAssignableFrom(typeof(T)))
        {
            tasks.Add(
                Session is null
                ? Collection<FileChunk<TId>>().DeleteManyAsync(x => IDs.Contains(x.FileID))
                : Collection<FileChunk<TId>>().DeleteManyAsync(Session, x => IDs.Contains(x.FileID), null, cancellation));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return await delResTask.ConfigureAwait(false);
    }


    /// <summary>
    /// Deletes a single entity from MongoDB
    /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <param name="ID">The Id of the entity to delete</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<DeleteResult> DeleteAsync<T, TId>(TId ID, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return DeleteAsync<T, TId>(Builders<T>.Filter.Eq(e => e.ID, ID), cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collection: collection, collectionName: collectionName);
    }

    /// <summary>
    /// Deletes matching entities from MongoDB
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <param name="IDs">An IEnumerable of entity IDs</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<DeleteResult> DeleteAsync<T, TId>(IEnumerable<TId> IDs, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return DeleteAsync<T, TId>(Builders<T>.Filter.In(e => e.ID, IDs), cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collection: collection, collectionName: collectionName);
    }

    /// <summary>
    /// Deletes matching entities from MongoDB
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <param name="expression">A lambda expression for matching entities to delete.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="collation">An optional collation object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<DeleteResult> DeleteAsync<T, TId>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default, Collation? collation = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return DeleteAsync<T, TId>(Builders<T>.Filter.Where(expression), cancellation, collation, ignoreGlobalFilters, collection: collection, collectionName: collectionName);
    }

    /// <summary>
    /// Deletes matching entities with a filter expression
    /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="collation">An optional collation object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<DeleteResult> DeleteAsync<T, TId>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default, Collation? collation = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return DeleteAsync<T, TId>(filter(Builders<T>.Filter), cancellation, collation, ignoreGlobalFilters, collection: collection, collectionName: collectionName);
    }

    /// <summary>
    /// Deletes matching entities with a filter definition
    /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <param name="filter">A filter definition for matching entities to delete.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="collation">An optional collation object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public async Task<DeleteResult> DeleteAsync<T, TId>(FilterDefinition<T> filter, CancellationToken cancellation = default, Collation? collation = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        ThrowIfCancellationNotSupported(cancellation);

        var filterDef = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, filter);
        var cursor = await new Find<T, TId, TId?>(this, Collection(collectionName, collection))
                           .Match(filter)
                           .Project(e => e.ID)
                           .Option(o => o.BatchSize = _deleteBatchSize)
                           .Option(o => o.Collation = collation)
                           .ExecuteCursorAsync(cancellation)
                           .ConfigureAwait(false);

        long deletedCount = 0;
        DeleteResult? res = null;

        using (cursor)
        {
            while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                if (cursor.Current.Any())
                {
                    res = await DeleteCascadingAsync<T, TId>(cursor.Current, cancellation).ConfigureAwait(false);
                    deletedCount += res.DeletedCount;
                }
            }
        }

        if (res?.IsAcknowledged == false)
            return DeleteResult.Unacknowledged.Instance;

        return new DeleteResult.Acknowledged(deletedCount);
    }
}
