using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task InsertAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            SetModifiedBySingle(entity);
            OnBeforeSave<T>()?.Invoke(entity);
            return DB.InsertAsync(entity, Session, cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing an existing entities or creating a new ones if they do not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task<BulkWriteResult<T>> InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            SetModifiedByMultiple(entities);
            foreach (var ent in entities) OnBeforeSave<T>()?.Invoke(ent);
            return DB.InsertAsync(entities, Session, cancellation);
        }
    }
}
