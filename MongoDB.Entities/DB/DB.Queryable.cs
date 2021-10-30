using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IQueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <param name="session">An optional session if used within a transaction</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoQueryable<T> Queryable<T>(AggregateOptions options = null, IClientSessionHandle session = null, string tenantPrefix = null) where T : IEntity
        {
            return session == null
                   ? Collection<T>(tenantPrefix).AsQueryable(options)
                   : Collection<T>(tenantPrefix).AsQueryable(session, options);
        }
    }
}
