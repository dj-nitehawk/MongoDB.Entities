using MongoDB.Driver;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Gets a fast estimation of how many documents are in the collection using metadata.
    /// <para>HINT: The estimation may not be exactly accurate.</para>
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="collectionName">To override the default collection name</param>
    /// <param name="collection">To override the default collection</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<long> CountEstimatedAsync<T>(CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
    {
        return Collection(collectionName, collection).EstimatedDocumentCountAsync(cancellationToken: cancellation);
    }

    /// <summary>
    /// Gets an accurate count of how many entities are matched for a given expression/filter
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<long> CountAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default, CountOptions? options = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
    {
        return CountAsync((FilterDefinition<T>)expression, cancellation, options, ignoreGlobalFilters, collection: collection, collectionName: collectionName);
    }

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<long> CountAsync<T>(CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
    {
        return CountAsync<T>(_ => true, cancellation, collectionName: collectionName, collection: collection);

    }

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="filter">A filter definition</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellation = default, CountOptions? options = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
    {
        filter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, filter);
        return
             Session == null
             ? Collection(collectionName, collection).CountDocumentsAsync(filter, options, cancellation)
             : Collection(collectionName, collection).CountDocumentsAsync(Session, filter, options, cancellation);
    }

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default, CountOptions? options = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
    {
        return CountAsync(filter(Builders<T>.Filter), cancellation, options, ignoreGlobalFilters, collectionName: collectionName, collection: collection);
    }
}
