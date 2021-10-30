using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        //NOTE: transaction support will not be added due to unpredictability with concurrency.

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<ulong> NextSequentialNumberAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return DB.NextSequentialNumberAsync(DB.CollectionName<T>(), tenantPrefix, cancellation);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given sequence name everytime the method is called
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get the next number for</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<ulong> NextSequentialNumberAsync(string sequenceName, CancellationToken cancellation = default)
        {
            return DB.NextSequentialNumberAsync(sequenceName, tenantPrefix, cancellation);
        }
    }
}
