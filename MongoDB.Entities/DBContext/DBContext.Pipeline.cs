using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

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
    public Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false) where T : IEntity
    {
        return DB.PipelineCursorAsync(MergeGlobalFilter(template, ignoreGlobalFilters), options, Session, cancellation);
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
    public Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false) where T : IEntity
    {
        return DB.PipelineAsync(MergeGlobalFilter(template, ignoreGlobalFilters), options, Session, cancellation);
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
    public Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false) where T : IEntity
    {
        return DB.PipelineSingleAsync(MergeGlobalFilter(template, ignoreGlobalFilters), options, Session, cancellation);
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
    public Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default, bool ignoreGlobalFilters = false) where T : IEntity
    {
        return DB.PipelineFirstAsync(MergeGlobalFilter(template, ignoreGlobalFilters), options, Session, cancellation);
    }

    private Template<T, TResult> MergeGlobalFilter<T, TResult>(Template<T, TResult> template, bool ignoreGlobalFilters) where T : IEntity
    {
        //WARNING: this has to do the same thing as Logic.MergeGlobalFilter method
        //         if the following logic changes, update the other method also

        if (!ignoreGlobalFilters && globalFilters.Count > 0 && globalFilters.TryGetValue(typeof(T), out var gFilter))
        {
            BsonDocument filter = null;

            switch (gFilter.filterDef)
            {
                case FilterDefinition<T> def:
                    filter = def.Render(
                        BsonSerializer.SerializerRegistry.GetSerializer<T>(),
                        BsonSerializer.SerializerRegistry,
                        Driver.Linq.LinqProvider.V3);
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
