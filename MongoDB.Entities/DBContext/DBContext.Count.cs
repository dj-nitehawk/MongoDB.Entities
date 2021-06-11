using MongoDB.Driver;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Gets a fast estimation of how many documents are in the collection using metadata.
        /// <para>HINT: The estimation may not be exactly accurate.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountEstimatedAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountEstimatedAsync<T>(cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many entities are matched for a given expression/filter
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="options">An optional CountOptions object</param>
        public virtual Task<long> CountAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default, CountOptions options = null) where T : IEntity
        {
            return DB.CountAsync(MergeWithGlobalFilter<T>(expression), session, cancellation, options);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync<T>(session, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">A filter definition</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="options">An optional CountOptions object</param>
        public virtual Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellation = default, CountOptions options = null) where T : IEntity
        {
            return DB.CountAsync(MergeWithGlobalFilter(filter), session, cancellation, options);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="options">An optional CountOptions object</param>
        public virtual Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default, CountOptions options = null) where T : IEntity
        {
            return DB.CountAsync(MergeWithGlobalFilter(filter(Builders<T>.Filter)), session, cancellation, options);
        }
    }
}
