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
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoQueryable<T> Queryable<T>(AggregateOptions? options = null) where T : IEntity
        {
            return Context.Queryable<T>(options);
        }
    }
}
