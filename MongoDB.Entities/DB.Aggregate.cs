using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Executes an aggregation framework pipeline by supplying a 'Template' object
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static IAsyncCursor<TResult> Aggregate<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return session == null
                   ? Collection<T>().Aggregate(template.ToPipeline(), options, cancellation)
                   : Collection<T>().Aggregate(session, template.ToPipeline(), options, cancellation);
        }

        /// <summary>
        /// Executes an aggregation framework pipeline by supplying a 'Template' object
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<IAsyncCursor<TResult>> AggregateAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return session == null
                   ? Collection<T>().AggregateAsync(template.ToPipeline(), options, cancellation)
                   : Collection<T>().AggregateAsync(session, template.ToPipeline(), options, cancellation);
        }
    }
}
