using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public static partial class DB
{
    /// <summary>
    /// Gets a fast estimation of how many documents are in the collection using metadata.
    /// <para>HINT: The estimation may not be exactly accurate.</para>
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<long> CountEstimatedAsync<T>(CancellationToken cancellation = default) where T : IEntity
        => Collection<T>().EstimatedDocumentCountAsync(null, cancellation);

    /// <summary>
    /// Gets an accurate count of how many entities are matched for a given expression/filter
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    public static Task<long> CountAsync<T>(Expression<Func<T, bool>> expression,
                                           IClientSessionHandle? session = null,
                                           CancellationToken cancellation = default,
                                           CountOptions? options = null) where T : IEntity
        => session == null
               ? Collection<T>().CountDocumentsAsync(expression, options, cancellation)
               : Collection<T>().CountDocumentsAsync(session, expression, options, cancellation);

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="filter">A filter definition</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    public static Task<long> CountAsync<T>(FilterDefinition<T> filter,
                                           IClientSessionHandle? session = null,
                                           CancellationToken cancellation = default,
                                           CountOptions? options = null) where T : IEntity
        => session == null
               ? Collection<T>().CountDocumentsAsync(filter, options, cancellation)
               : Collection<T>().CountDocumentsAsync(session, filter, options, cancellation);

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="options">An optional CountOptions object</param>
    public static Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter,
                                           IClientSessionHandle? session = null,
                                           CancellationToken cancellation = default,
                                           CountOptions? options = null) where T : IEntity
        => session == null
               ? Collection<T>().CountDocumentsAsync(filter(Builders<T>.Filter), options, cancellation)
               : Collection<T>().CountDocumentsAsync(session, filter(Builders<T>.Filter), options, cancellation);

    /// <summary>
    /// Gets an accurate count of how many total entities are in the collection for a given entity type
    /// </summary>
    /// <typeparam name="T">The entity type to get the count for</typeparam>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<long> CountAsync<T>(IClientSessionHandle? session = null, CancellationToken cancellation = default) where T : IEntity
    {
        return CountAsync<T>(_ => true, session, cancellation);
    }
}