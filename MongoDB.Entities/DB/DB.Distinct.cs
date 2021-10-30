using MongoDB.Driver;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Represents a MongoDB Distinct command where you can get back distinct values for a given property of a given Entity.
        /// </summary>
        /// <typeparam name="T">Any Entity that implements IEntity interface</typeparam>
        /// <typeparam name="TProperty">The type of the property of the entity you'd like to get unique values for</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Distinct<T, TProperty> Distinct<T, TProperty>(string tenantPrefix, IClientSessionHandle session = null) where T : IEntity
            => new(session, null, tenantPrefix);
    }
}
