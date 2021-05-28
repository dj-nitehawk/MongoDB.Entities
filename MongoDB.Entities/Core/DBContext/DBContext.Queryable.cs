using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IMongoQueryable<T> Queryable<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Queryable<T>(options, session);
        }
    }
}
