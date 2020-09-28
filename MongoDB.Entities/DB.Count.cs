using MongoDB.Driver;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB //todo: add count methods to documentation
    {
        /// <summary>
        /// Gets a fast estimation of how many documents are in the collection using metadata.
        /// <para>HINT: The estimation may not be exactly accurate.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<long> CountEstimatedAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return Collection<T>().EstimatedDocumentCountAsync(null, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many entities are matched for a given expression/filter
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<long> CountAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return
                 session == null
                 ? Collection<T>().CountDocumentsAsync(expression, null, cancellation)
                 : Collection<T>().CountDocumentsAsync(session, expression, null, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<long> CountAsync<T>(IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return CountAsync<T>(_ => true, session, cancellation);
        }
    }
}
