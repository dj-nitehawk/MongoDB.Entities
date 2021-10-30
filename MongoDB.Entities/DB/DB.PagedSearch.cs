using MongoDB.Driver;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Represents an aggregation query that retrieves results with easy paging support.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static PagedSearch<T> PagedSearch<T>(string tenantPrefix, IClientSessionHandle session = null) where T : IEntity
            => new(session, null, tenantPrefix);

        /// <summary>
        /// Represents an aggregation query that retrieves results with easy paging support.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public static PagedSearch<T, TProjection> PagedSearch<T, TProjection>(string tenantPrefix, IClientSessionHandle session = null) where T : IEntity
            => new(session, null, tenantPrefix);
    }
}
