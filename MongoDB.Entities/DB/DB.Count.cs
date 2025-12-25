using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Gets a fast estimation of how many documents are in the collection using metadata.
    /// <para>HINT: The estimation may not be exactly accurate.</para>
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<long> CountEstimatedAsync<T>(CancellationToken cancellation = default) where T : IEntity
        => Collection<T>().EstimatedDocumentCountAsync(null, cancellation);

    /// <summary>
    /// Gets an accurate count of how many entities are matched for a given expression/filter
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    public Task<long> CountAsync<T>(Expression<Func<T, bool>> expression,
                                    CancellationToken cancellation = default,
                                    CountOptions? options = null,
                                    bool ignoreGlobalFilters = false) where T : IEntity
    {
        var filter = Logic.MergeWithGlobalFilter<T>(ignoreGlobalFilters, _globalFilters, expression);

        return SessionHandle is null
                   ? Collection<T>().CountDocumentsAsync(filter, options, cancellation)
                   : Collection<T>().CountDocumentsAsync(SessionHandle, filter, options, cancellation);
    }

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="filter">A filter definition</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    public Task<long> CountAsync<T>(FilterDefinition<T> filter,
                                    CancellationToken cancellation = default,
                                    CountOptions? options = null,
                                    bool ignoreGlobalFilters = false) where T : IEntity
    {
        var f = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, filter);

        return SessionHandle is null
                   ? Collection<T>().CountDocumentsAsync(f, options, cancellation)
                   : Collection<T>().CountDocumentsAsync(SessionHandle, f, options, cancellation);
    }

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    public Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter,
                                    CancellationToken cancellation = default,
                                    CountOptions? options = null,
                                    bool ignoreGlobalFilters = false) where T : IEntity
    {
        var f = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, filter(Builders<T>.Filter));

        return SessionHandle is null
                   ? Collection<T>().CountDocumentsAsync(f, options, cancellation)
                   : Collection<T>().CountDocumentsAsync(SessionHandle, f, options, cancellation);
    }

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<long> CountAsync<T>(CancellationToken cancellation = default) where T : IEntity
        => CountAsync<T>(_ => true, cancellation);
}