using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and returns a cursor
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.PipelineCursorAsync(template, options, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and get a list of results
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.PipelineAsync(template, options, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and get a single result or default value if not found. 
        /// If more than one entity is found, it will throw an exception.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting object</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.PipelineSingleAsync(template, options, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and get the first result or default value if not found.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting object</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.PipelineFirstAsync(template, options, cancellation);

        }
    }
}
