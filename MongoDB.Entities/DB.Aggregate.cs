using MongoDB.Driver;
using MongoDB.Entities.Core;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        //todo: write tests for these
        //todo: TN.Aggreate...

        /// <summary>
        /// Executes an aggregation framework pipeline by supplying a 'Template' object
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static IAsyncCursor<TResult> Aggregate<T, TResult>(Template template, AggregateOptions options = null, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return session == null
                   ? Collection<T>(db).Aggregate(template.ToPipeline<T, TResult>(), options)
                   : Collection<T>(db).Aggregate(session, template.ToPipeline<T, TResult>(), options);
        }

        /// <summary>
        /// Executes an aggregation framework pipeline by supplying a 'Template' object
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public IAsyncCursor<TResult> Aggregate<T, TResult>(Template template, AggregateOptions options = null, IClientSessionHandle session = null) where T : IEntity
        {
            return Aggregate<T, TResult>(template, options, session, DbName);
        }

        /// <summary>
        /// Executes an aggregation framework pipeline by supplying a 'Template' object
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Task<IAsyncCursor<TResult>> AggregateAsync<T, TResult>(Template template, AggregateOptions options = null, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return session == null
                   ? Collection<T>(db).AggregateAsync(template.ToPipeline<T, TResult>(), options)
                   : Collection<T>(db).AggregateAsync(session, template.ToPipeline<T, TResult>(), options);
        }

        /// <summary>
        /// Executes an aggregation framework pipeline by supplying a 'Template' object
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public Task<IAsyncCursor<TResult>> AggregateAsync<T, TResult>(Template template, AggregateOptions options = null, IClientSessionHandle session = null) where T : IEntity
        {
            return AggregateAsync<T, TResult>(template, options, session, DbName);
        }
    }
}
