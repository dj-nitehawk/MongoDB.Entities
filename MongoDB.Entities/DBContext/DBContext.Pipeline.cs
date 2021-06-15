using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
            return DB.PipelineCursorAsync(MergeGlobalFilter(template), options, session, cancellation);
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
            return DB.PipelineAsync(MergeGlobalFilter(template), options, session, cancellation);
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
            return DB.PipelineSingleAsync(MergeGlobalFilter(template), options, session, cancellation);
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
            return DB.PipelineFirstAsync(MergeGlobalFilter(template), options, session, cancellation);
        }

        private Template<T, TResult> MergeGlobalFilter<T, TResult>(Template<T, TResult> template) where T : IEntity
        {
            if (globalFilters.Count > 0 && globalFilters.TryGetValue(typeof(T), out var gFilter))
            {
                BsonDocument filter = null;

                switch (gFilter.filterDef)
                {
                    case FilterDefinition<T> def:
                        filter = def.Render(
                            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
                            BsonSerializer.SerializerRegistry);
                        break;

                    case string jsonString:
                        filter = BsonDocument.Parse(jsonString);
                        break;
                }

                if (gFilter.prepend) template.builder.Insert(1, $"{{$match:{filter}}},");
                else template.builder.Insert(template.builder.Length - 1, $",{{$match:{filter}}}");
            }
            return template;
        }
    }
}
