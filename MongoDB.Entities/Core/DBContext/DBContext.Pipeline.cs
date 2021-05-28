using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object.
        /// Gets a cursor back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineCursorAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object.
        /// Gets a list back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object.
        /// Gets a single or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineSingleAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object.
        /// Gets the first or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineFirstAsync(template, options, session, cancellation);
        }
    }
}
