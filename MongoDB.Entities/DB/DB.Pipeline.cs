using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Executes an aggregation pipeline by supplying a 'Template' object and returns a cursor
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TResult">The type of the resulting objects</typeparam>
    /// <param name="template">A 'Template' object with tags replaced</param>
    /// <param name="options">The options for the aggregation. This is not required.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template,
                                                                       CancellationToken cancellation = default,
                                                                       AggregateOptions? options = null) where T : IEntity
        => Session == null
               ? Collection<T>().AggregateAsync(MergeGlobalFilter(template).ToPipeline(), options, cancellation)
               : Collection<T>().AggregateAsync(Session, MergeGlobalFilter(template).ToPipeline(), options, cancellation);

    /// <summary>
    /// Executes an aggregation pipeline by supplying a 'Template' object and get a list of results
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TResult">The type of the resulting objects</typeparam>
    /// <param name="template">A 'Template' object with tags replaced</param>
    /// <param name="options">The options for the aggregation. This is not required.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template,
                                                               CancellationToken cancellation = default,
                                                               AggregateOptions? options = null) where T : IEntity
    {
        var list = new List<TResult>();
        using var cursor = await PipelineCursorAsync(template, cancellation, options).ConfigureAwait(false);

        while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            list.AddRange(cursor.Current);

        return list;
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
    public async Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template,
                                                               CancellationToken cancellation = default,
                                                               AggregateOptions? options = null) where T : IEntity
    {
        var opts = options ?? new AggregateOptions();
        opts.BatchSize = 2;

        using var cursor = await PipelineCursorAsync(template, cancellation, opts).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);

        return cursor.Current.SingleOrDefault();
    }

    /// <summary>
    /// Executes an aggregation pipeline by supplying a 'Template' object and get the first result or default value if not found.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TResult">The type of the resulting object</typeparam>
    /// <param name="template">A 'Template' object with tags replaced</param>
    /// <param name="options">The options for the aggregation. This is not required.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template,
                                                              CancellationToken cancellation = default,
                                                              AggregateOptions? options = null) where T : IEntity
    {
        var opts = options ?? new AggregateOptions();
        opts.BatchSize = 1;

        using var cursor = await PipelineCursorAsync(template, cancellation, opts).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);

        return cursor.Current.SingleOrDefault();
    }

    Template<T, TResult> MergeGlobalFilter<T, TResult>(Template<T, TResult> template) where T : IEntity
    {
        //WARNING: this has to do the same thing as Logic.MergeGlobalFilter method
        //         if the following logic changes, update the other method also

        if (IgnoreGlobalFilters || !(_globalFilters?.Count > 0) || !_globalFilters.TryGetValue(typeof(T), out var gFilter))
            return template;

        var filter = gFilter.filterDef switch
        {
            FilterDefinition<T> def => def.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)),
            BsonDocument doc => doc,
            string jsonString => BsonDocument.Parse(jsonString),
            _ => null
        };

        if (gFilter.prepend)
            template.Builder.Insert(1, $"{{$match:{filter}}},");
        else
            template.Builder.Insert(template.Builder.Length - 1, $",{{$match:{filter}}}");

        return template;
    }
}