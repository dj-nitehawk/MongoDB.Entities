using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Deletes a single entity from MongoDB.
    /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="db">The DBInstance to use for this operation</param>
    /// <param name="session"></param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<DeleteResult> DeleteAsync<T>(this T entity, DB? db = null, IClientSessionHandle? session = null, CancellationToken cancellation = default)
        where T : IEntity
        => DB.InstanceOrDefault(db).DeleteAsync<T>(entity.GetId(), session, cancellation);

    /// <summary>
    /// Deletes multiple entities from the database
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="db">The DBInstance to use for this operation</param>
    /// <param name="session"></param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<DeleteResult> DeleteAllAsync<T>(this IEnumerable<T> entities,
                                                       DB? db = null,
                                                       IClientSessionHandle? session = null,
                                                       CancellationToken cancellation = default) where T : IEntity
        => DB.InstanceOrDefault(db).DeleteAsync<T>(entities.Select(Cache<T>.IdSelector), session, cancellation);
}