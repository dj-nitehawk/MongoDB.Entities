using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class Extensions
    {
        /// <summary>
        /// Inserts a new entity into the colleciton.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public static Task InsertAsync<T>(this T entity, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return DB.Context.InsertAsync(entity, cancellation, collectionName, collection);
        }

        /// <summary>
        /// Inserts a batch of new entities into the collection.
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public static Task<BulkWriteResult<T>> InsertAsync<T>(this IEnumerable<T> entities, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return DB.Context.InsertAsync(entities, cancellation, collectionName: collectionName, collection: collection);
        }

    }
}
