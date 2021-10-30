using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class Extensions
    {
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="session"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<DeleteResult> DeleteAsync<T>(this T entity, string tenantPrefix, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync<T>(entity.ID, tenantPrefix, session, cancellation);
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="session"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<DeleteResult> DeleteAllAsync<T>(this IEnumerable<T> entities, string tenantPrefix, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync<T>(entities.Select(e => e.ID), tenantPrefix, session, cancellation);
        }

    }
}
