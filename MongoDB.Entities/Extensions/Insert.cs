using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Inserts a new entity into the collection.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="db">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task InsertAsync<T>(this T entity, DB? db = null, IClientSessionHandle? session = null, CancellationToken cancellation = default) where T : IEntity
        => DB.InstanceOrDefault(db).InsertAsync(entity, session, cancellation);

    /// <summary>
    /// Inserts a batch of new entities into the collection.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="db">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<BulkWriteResult<T>> InsertAsync<T>(this IEnumerable<T> entities,
                                                          DB? db = null,
                                                          IClientSessionHandle? session = null,
                                                          CancellationToken cancellation = default) where T : IEntity
        => DB.InstanceOrDefault(db).InsertAsync(entities, session, cancellation);
}