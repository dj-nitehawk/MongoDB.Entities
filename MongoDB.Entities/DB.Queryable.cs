using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using System.Linq;

namespace MongoDB.Entities
{
    public partial class DB
    {
        //todo: add transaction support for Queryable<T> when driver supports it.

        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IQueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoQueryable<T> Queryable<T>(AggregateOptions options = null) where T : IEntity, new() => Collection<T>().AsQueryable(options);

        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IQueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public IMongoQueryable<T> Queryable<T>(AggregateOptions options = null, string db = null) where T : IEntity => Queryable<T>(options);
    }
}
