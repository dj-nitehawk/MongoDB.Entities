using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        {
            template = MergeTemplateGlobalFilter(template, ignoreGlobalFilters);
            return Session == null
                ? Collection(collectionName, collection).AggregateAsync(template.ToPipeline(), options, cancellation)
                : Collection(collectionName, collection).AggregateAsync(Session, template.ToPipeline(), options, cancellation);
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
        /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public async Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        {
            var list = new List<TResult>();
            using (var cursor = await PipelineCursorAsync(template, options, cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection).ConfigureAwait(false))
            {
                while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
                {
                    list.AddRange(cursor.Current);
                }
            }
            return list;
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
        /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public async Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        {

            AggregateOptions opts = options ?? new AggregateOptions();
            opts.BatchSize = 2;

            using var cursor = await PipelineCursorAsync(template, opts, cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection).ConfigureAwait(false);
            await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
            return cursor.Current.SingleOrDefault();
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
        /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public async Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template, AggregateOptions? options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
        {

            var opts = options ?? new AggregateOptions();
            opts.BatchSize = 1;

            using var cursor = await PipelineCursorAsync(template, opts, cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection).ConfigureAwait(false);
            await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
            return cursor.Current.SingleOrDefault();
        }

        private Template<T, TResult> MergeTemplateGlobalFilter<T, TResult>(Template<T, TResult> template, bool ignoreGlobalFilters)
        {
            //WARNING: this has to do the same thing as Logic.MergeGlobalFilter method
            //         if the following logic changes, update the other method also

            if (!ignoreGlobalFilters && GlobalFilters.Count > 0 && GlobalFilters.TryGetValue(typeof(T), out var gFilter))
            {
                BsonDocument? filter = null;

                switch (gFilter.filterDef)
                {
                    case FilterDefinition<T> def:
                        filter = def.Render(
                            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
                            BsonSerializer.SerializerRegistry);
                        break;

                    case BsonDocument doc:
                        filter = doc;
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
