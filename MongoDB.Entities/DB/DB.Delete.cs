using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    const int DeleteBatchSize = 100000;

    // ReSharper disable once InconsistentNaming
    async Task<DeleteResult> DeleteCascadingAsync<T>(IEnumerable<object?> IDs, CancellationToken cancellation, bool idsAreStoredValues = false) where T : IEntity
    {
        // note: cancellation should not be enabled outside of transactions because multiple collections are involved 
        //       and premature cancellation could cause data inconsistencies.
        //       i.e. don't pass the cancellation token to delete methods below that don't take a session.
        //       also make consumers call ThrowIfCancellationNotSupported() before calling this method.

        ThrowIfCancellationNotSupported(SessionHandle, cancellation);

        var ids = IDs.ToArray();

        //join records hold the stored representation of entity IDs, so raw CLR values must be
        //converted through the ID member's serializer before matching against them
        var storedIds = ids.Select(
                                id => idsAreStoredValues
                                          ? id as BsonValue ?? BsonValue.Create(id)
                                          : Cache<T>.IdToBsonValue(id))
                           .ToArray();

        // Direct-ID deletes must not cascade join/file side-effects for entities excluded by a
        // global filter. Resolve the eligible ID set first (raw BSON projection) and use only that.
        var hasGlobalFilter = !IgnoreGlobalFilters && _globalFilters?.Count > 0 && _globalFilters.ContainsKey(typeof(T));
        if (hasGlobalFilter)
        {
            var eligibilityFilter = Logic.MergeWithGlobalFilter(false, _globalFilters, IDInFilter<T>(storedIds));

            var matched = await new Find<T, BsonDocument>(SessionHandle, null, this)
                                .Match(eligibilityFilter)
                                .Project(p => p.Include(Cache<T>.IdPropName))
                                .ExecuteAsync(cancellation)
                                .ConfigureAwait(false);

            if (matched.Count == 0)
                return new DeleteResult.Acknowledged(0);

            var idElement = Cache<T>.IdBsonName;
            storedIds = matched.Select(doc => doc[idElement]).ToArray();
            ids = matched.Select(doc => (object)doc[idElement]).ToArray();
        }

        // File deletes also recheck this filter when conditionally deleting metadata.
        var filter = Logic.MergeWithGlobalFilter(IgnoreGlobalFilters, _globalFilters, IDInFilter<T>(storedIds));
        Task<DeleteResult>? delResTask = null;
        var isFile = typeof(IFileEntity).IsAssignableFrom(typeof(T));
        if (isFile)
        {
            // Validate the entire batch before starting any entity, relationship, or chunk deletes.
            foreach (var id in ids)
                FileChunk.ValidateFileID(id?.ToString());
            // A concurrent upload can make a conditional file delete skip metadata.
            // Only cascade relationships for files whose metadata was actually deleted.
            storedIds = await FileStorage.DeleteAsync(this, filter, SessionHandle, cancellation).ConfigureAwait(false);
            if (storedIds.Length == 0)
                return new DeleteResult.Acknowledged(0);
            delResTask = Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(storedIds.Length));
        }

        else if (hasGlobalFilter)
        {
            // Eligibility is only a snapshot. Atomically recheck each entity's filter and
            // retain its stored ID only when the server actually deletes that document.
            var deletedIds = new List<BsonValue>();
            var options = new FindOneAndDeleteOptions<T, BsonDocument>
            {
                Projection = new BsonDocument(Cache<T>.IdBsonName, 1)
            };
            var collection = Collection<T>();
            foreach (var id in storedIds.Distinct())
            {
                var conditionalFilter = Logic.MergeWithGlobalFilter(
                    false, _globalFilters, new BsonDocumentFilterDefinition<T>(new(Cache<T>.IdBsonName, id)));
                // ReSharper disable once MethodSupportsCancellation
                var deleted = SessionHandle == null
                                  ? await collection.FindOneAndDeleteAsync(conditionalFilter, options).ConfigureAwait(false)
                                  : await collection.FindOneAndDeleteAsync(SessionHandle, conditionalFilter, options, cancellation).ConfigureAwait(false);
                if (deleted != null)
                    deletedIds.Add(deleted[Cache<T>.IdBsonName]);
            }
            storedIds = deletedIds.ToArray();
            if (storedIds.Length == 0)
                return new DeleteResult.Acknowledged(0);
            delResTask = Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(storedIds.Length));
        }

        var tasks = new List<Task>();

        var parentFilter = Builders<JoinRecord>.Filter.In(r => r.ParentID, storedIds);
        var childFilter = Builders<JoinRecord>.Filter.In(r => r.ChildID, storedIds);

        foreach (var reference in Cache<T>.ReferenceCollections)
        {
            var refCollection = GetRefCollection(reference.Key);
            var joinRecordFilter = reference.Value switch
            {
                JoinRecordRole.Parent => parentFilter,
                JoinRecordRole.Child => childFilter,
                JoinRecordRole.Both => Builders<JoinRecord>.Filter.Or(parentFilter, childFilter),
                _ => throw new InvalidOperationException("Unknown join record role.")
            };

            // ReSharper disable once MethodSupportsCancellation
            tasks.Add(
                SessionHandle == null
                    ? refCollection.DeleteManyAsync(joinRecordFilter)
                    : refCollection.DeleteManyAsync(SessionHandle, joinRecordFilter, null, cancellation));
        }

        // ReSharper disable once MethodSupportsCancellation
        delResTask ??= SessionHandle == null
                          ? Collection<T>().DeleteManyAsync(filter)
                          : Collection<T>().DeleteManyAsync(SessionHandle, filter, null, cancellation);

        tasks.Add(delResTask);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return await delResTask.ConfigureAwait(false);
    }

    static FilterDefinition<T> IDInFilter<T>(IEnumerable<BsonValue> storedIds) where T : IEntity
        => new BsonDocumentFilterDefinition<T>(new(Cache<T>.IdBsonName, new BsonDocument("$in", new BsonArray(storedIds))));

    internal static BsonValue ToBsonValue(BsonMemberMap memberMap, object? value)
    {
        var document = new BsonDocument();
        var memberValue = ToMemberValue(memberMap, value);

        using (var writer = new BsonDocumentWriter(document))
        {
            var context = BsonSerializationContext.CreateRoot(writer);

            writer.WriteStartDocument();
            writer.WriteName(memberMap.ElementName);
            memberMap.GetSerializer().Serialize(context, new() { NominalType = memberMap.MemberType }, memberValue);
            writer.WriteEndDocument();
        }

        return document[memberMap.ElementName];
    }

    static object? ToMemberValue(BsonMemberMap memberMap, object? value)
    {
        if (value == null || memberMap.MemberType.IsInstanceOfType(value))
            return value;

        var document = new BsonDocument(memberMap.ElementName, value as BsonValue ?? BsonValue.Create(value));

        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName(memberMap.ElementName);

        return memberMap.GetSerializer().Deserialize(context, new() { NominalType = memberMap.MemberType });
    }

    /// <summary>
    /// Deletes a single entity from MongoDB.
    /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to delete</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<DeleteResult> DeleteAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        => DeleteCascadingAsync<T>([entity.GetId()], cancellation);

    /// <summary>
    /// Deletes a single entity from MongoDB.
    /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
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
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
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
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="IDs">An IEnumerable of entity IDs</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<DeleteResult> DeleteAsync<T>(IEnumerable<object?> IDs, CancellationToken cancellation = default) where T : IEntity
        => DeleteByIdsAsync<T>(IDs, cancellation);

    /// <summary>
    /// Deletes entities using a collection of IDs of any CLR type (including value types such as <see cref="Guid"/>, <see cref="long"/>, and <see cref="ObjectId"/>).
    /// <para>HINT: If more than 100,000 IDs are passed in, they will be processed in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TId">The CLR type of the entity IDs</typeparam>
    /// <param name="IDs">An IEnumerable of entity IDs</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<DeleteResult> DeleteAsync<T, TId>(IReadOnlyList<TId> IDs, CancellationToken cancellation = default) where T : IEntity where TId : struct
        => DeleteByIdsAsync<T>(BoxIds(IDs), cancellation);

    async Task<DeleteResult> DeleteByIdsAsync<T>(IEnumerable<object?> IDs, CancellationToken cancellation) where T : IEntity
    {
        // materialize once so Count/batching/cascade never re-enumerate a live source sequence
        var ids = IDs as object?[] ?? IDs.ToArray();

        if (ids.Length <= DeleteBatchSize)
            return await DeleteCascadingAsync<T>(ids, cancellation).ConfigureAwait(false);

        long deletedCount = 0;
        DeleteResult res = DeleteResult.Unacknowledged.Instance;

        foreach (var batch in ids.ToBatches(DeleteBatchSize))
        {
            res = await DeleteCascadingAsync<T>(batch, cancellation).ConfigureAwait(false);
            deletedCount += res.DeletedCount;
        }

        if (res.IsAcknowledged)
            res = new DeleteResult.Acknowledged(deletedCount);

        return res;
    }

    static object?[] BoxIds<TId>(IEnumerable<TId> ids)
        => ids as object?[] ?? ids.Select(id => (object?)id).ToArray();

    /// <summary>
    /// Deletes matching entities with an expression
    /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
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
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
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
    /// <para>With an active global filter, non-file entities are conditionally deleted before their relationships.
    /// Entities skipped by that filter retain their relationships. This requires one delete round trip per candidate.</para>
    /// <para>Without a transaction, failures can leave partial deletions or orphaned relationships. Use a transaction
    /// for atomic cross-collection deletion; cleanup failures are propagated, not automatically rolled back.</para>
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

        // project IDs as raw BSON documents so Guid/ObjectId/custom-represented values are not
        // re-deserialized through ObjectSerializer (which rejects GuidRepresentation.Standard).
        var cursor = await new Find<T, BsonDocument>(SessionHandle, null, this)
                           .Match(_ => filter)
                           .Project(p => p.Include(Cache<T>.IdPropName))
                           .Option(o => o.BatchSize = DeleteBatchSize)
                           .Option(o => o.Collation = collation)
                           .ExecuteCursorAsync(cancellation)
                           .ConfigureAwait(false);

        long deletedCount = 0;
        DeleteResult res = DeleteResult.Unacknowledged.Instance;
        var idElement = Cache<T>.IdBsonName;

        using (cursor)
        {
            while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                if (!cursor.Current.Any())
                    continue;

                var storedIds = cursor.Current.Select(doc => (object)doc[idElement]).ToArray();
                res = await DeleteCascadingAsync<T>(storedIds, cancellation, idsAreStoredValues: true).ConfigureAwait(false);
                deletedCount += res.DeletedCount;
            }
        }

        if (res.IsAcknowledged)
            res = new DeleteResult.Acknowledged(deletedCount);

        return res;
    }

    static void ThrowIfCancellationNotSupported(IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        if (cancellation != CancellationToken.None && session == null)
            throw new NotSupportedException("Cancellation is only supported within transactions for delete operations!");
    }
}
