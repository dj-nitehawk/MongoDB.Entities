using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {

        /// <summary>
        /// Inserts a new entity into the colleciton.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task InsertAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.InsertAsync(entity, cancellation);
        }

        /// <summary>
        /// Inserts a batch of new entities into the collection.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<BulkWriteResult<T>> InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.InsertAsync(entities, cancellation);
        }
    }
}
