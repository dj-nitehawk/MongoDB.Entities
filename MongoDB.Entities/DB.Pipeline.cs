using MongoDB.Driver;
using System;
using System.Collections.Generic;
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
        public static Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return session == null
                   ? Collection<T>().AggregateAsync(template.ToPipeline(), options, cancellation)
                   : Collection<T>().AggregateAsync(session, template.ToPipeline(), options, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and get a list of results
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return await 
                    (await PipelineCursorAsync(template, options, session, cancellation).ConfigureAwait(false))
                    .ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and get a single result or default value if not found.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting object</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return await
                    (await PipelineCursorAsync(template, options, session, cancellation).ConfigureAwait(false))
                    .SingleOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an aggregation pipeline by supplying a 'Template' object and get the first result or default value if not found.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TResult">The type of the resulting object</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return await
                    (await PipelineCursorAsync(template, options, session, cancellation).ConfigureAwait(false))
                    .FirstOrDefaultAsync().ConfigureAwait(false);
        }
    }
}
