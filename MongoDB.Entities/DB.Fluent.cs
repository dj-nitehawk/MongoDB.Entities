using MongoDB.Driver;
using System.Linq;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IAggregateFluent in order to facilitate Fluent queries.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns></returns>
        public static IAggregateFluent<T> Fluent<T>(AggregateOptions options = null, IClientSessionHandle session = null, string db = null)
        {
            return session == null
                   ? Collection<T>(db).Aggregate(options)
                   : Collection<T>(db).Aggregate(session, options);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IAggregateFluent in order to facilitate Fluent queries.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns></returns>
        public IAggregateFluent<T> Fluent<T>(AggregateOptions options = null, IClientSessionHandle session = null)
        {
            return Fluent<T>(options, session, DbName);
        }
    }
}
