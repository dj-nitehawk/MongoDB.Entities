namespace MongoDB.Entities;

public interface IPagedSearchBuilder<T, TProjection, TSelf> : IProjectionBuilder<T, TProjection, TSelf>
    where TSelf : IPagedSearchBuilder<T, TProjection, TSelf>
{

}
public abstract class PagedSearchBase<T, TProjection, TSelf> : SortFilterQueryBase<T, TSelf>, IPagedSearchBuilder<T, TProjection, TSelf>
    where TSelf : PagedSearchBase<T, TProjection, TSelf>
{
    internal PagedSearchBase(PagedSearchBase<T, TProjection, TSelf> other) : base(other)
    {
    }

    internal PagedSearchBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters)
    {
    }
    private TSelf This => (TSelf)this;

    internal IAggregateFluent<T>? _fluentPipeline;
    internal AggregateOptions _options = new();
    internal PipelineStageDefinition<T, TProjection>? _projectionStage;
    internal int _pageNumber = 1, _pageSize = 100;

    /// <summary>
    /// Begins the paged search aggregation pipeline with the provided fluent pipeline.
    /// <para>TIP: This method must be first in the chain and it cannot be used with .Match()</para>
    /// </summary>
    /// <typeparam name="TFluent">The type of the input pipeline</typeparam>
    /// <param name="fluentPipeline">The input IAggregateFluent pipeline</param>
    public TSelf WithFluent<TFluent>(TFluent fluentPipeline) where TFluent : IAggregateFluent<T>
    {
        this._fluentPipeline = fluentPipeline;
        return This;
    }





    /// <summary>
    /// Sort the results of a text search by the MetaTextScore
    /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
    /// </summary>
    public TSelf SortByTextScore()
    {
        return SortByTextScore(null);
    }

    /// <summary>
    /// Sort the results of a text search by the MetaTextScore and get back the score as well
    /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
    /// </summary>
    /// <param name="scoreProperty">x => x.TextScoreProp</param>
    public TSelf SortByTextScore(Expression<Func<T, object>>? scoreProperty)
    {
        switch (scoreProperty)
        {
            case null:
                AddTxtScoreToProjection("_Text_Match_Score_");
                return Sort(s => s.MetaTextScore("_Text_Match_Score_"));

            default:
                AddTxtScoreToProjection(Prop.Path(scoreProperty));
                return Sort(s => s.MetaTextScore(Prop.Path(scoreProperty)));
        }
    }

    private void AddTxtScoreToProjection(string fieldName)
    {
        if (_projectionStage == null)
        {
            _projectionStage = $"{{ $set : {{ {fieldName} : {{ $meta : 'textScore' }}  }} }}";
            return;
        }

        var renderedStage = _projectionStage.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry);

        renderedStage.Document["$project"][fieldName] = new BsonDocument { { "$meta", "textScore" } };

        _projectionStage = renderedStage.Document;
    }



    /// <summary>
    /// Specify the page number to get
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    public TSelf PageNumber(int pageNumber)
    {
        this._pageNumber = pageNumber;
        return This;
    }

    /// <summary>
    /// Specify the number of items per page
    /// </summary>
    /// <param name="pageSize">The size of a page</param>
    public TSelf PageSize(int pageSize)
    {
        this._pageSize = pageSize;
        return This;
    }

    /// <summary>
    /// Specify how to project the results using a lambda expression
    /// </summary>
    /// <param name="expression">x => new Test { PropName = x.Prop }</param>
    public TSelf Project(Expression<Func<T, TProjection>> expression)
    {
        _projectionStage = PipelineStageDefinitionBuilder.Project(expression);
        return This;
    }

    /// <summary>
    /// Specify how to project the results using a projection expression
    /// </summary>
    /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
    public TSelf Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
    {
        _projectionStage = PipelineStageDefinitionBuilder.Project(projection(Builders<T>.Projection));
        return This;
    }

    /// <summary>
    /// Specify how to project the results using an exclusion projection expression.
    /// </summary>
    /// <param name="exclusion">x => new { x.PropToExclude, x.AnotherPropToExclude }</param>
    public TSelf ProjectExcluding(Expression<Func<T, object>> exclusion)
    {
        var props = (exclusion.Body as NewExpression)?.Arguments
            .Select(a => a.ToString().Split('.')[1]);

        if (props == null || !props.Any())
            throw new ArgumentException("Unable to get any properties from the exclusion expression!");

        var defs = new List<ProjectionDefinition<T>>(props.Count());

        foreach (var prop in props)
        {
            defs.Add(Builders<T>.Projection.Exclude(prop));
        }

        _projectionStage = PipelineStageDefinitionBuilder.Project<T, TProjection>(Builders<T>.Projection.Combine(defs));

        return This;
    }

    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public TSelf Option(Action<AggregateOptions> option)
    {
        option(_options);
        return This;
    }


}

/// <summary>
/// Represents an aggregation query that retrieves results with easy paging support.
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class PagedSearch<T> : PagedSearch<T, T>
{
    internal PagedSearch(
        DBContext context, IMongoCollection<T> collection)
    : base(context, collection) { }
}

/// <summary>
/// Represents an aggregation query that retrieves results with easy paging support.
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
public class PagedSearch<T, TProjection> : PagedSearchBase<T, TProjection, PagedSearch<T, TProjection>>, ICollectionRelated<T>
{

    public DBContext Context { get; set; }
    public IMongoCollection<T> Collection { get; set; }

    internal PagedSearch(DBContext context, IMongoCollection<T> collection) : base(context.GlobalFilters)
    {
        var type = typeof(TProjection);
        if (type.IsPrimitive || type.IsValueType || (type == typeof(string)))
            throw new NotSupportedException("Projecting to primitive types is not supported!");
        Context = context;
        Collection = collection;
    }


    public PagedSearch<T, TProjection> IncludeRequiredProps()
    {
        _projectionStage = PipelineStageDefinitionBuilder.Project(this.Cache().CombineWithRequiredProps<TProjection>(_projectionStage?.ToString()));
        return this;
    }

    /// <summary>
    /// Run the aggregation search command in MongoDB server and get a page of results and total + page count
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<(IReadOnlyList<TProjection> Results, long TotalCount, int PageCount)> ExecuteAsync(CancellationToken cancellation = default)
    {
        if (_filter != Builders<T>.Filter.Empty && _fluentPipeline != null)
            throw new InvalidOperationException(".Match() and .WithFluent() cannot be used together!");

        var pipelineStages = new List<IPipelineStageDefinition>(4);

        if (_sorts.Count == 0)
            throw new InvalidOperationException("Paging without sorting is a sin!");
        else
            pipelineStages.Add(PipelineStageDefinitionBuilder.Sort(Builders<T>.Sort.Combine(_sorts)));

        pipelineStages.Add(PipelineStageDefinitionBuilder.Skip<T>((_pageNumber - 1) * _pageSize));
        pipelineStages.Add(PipelineStageDefinitionBuilder.Limit<T>(_pageSize));

        if (_projectionStage != null)
            pipelineStages.Add(_projectionStage);

        var resultsFacet = AggregateFacet.Create<T, TProjection>("_results", pipelineStages);

        var countFacet = AggregateFacet.Create("_count",
            PipelineDefinition<T, AggregateCountResult>.Create(new[]
            {
                    PipelineStageDefinitionBuilder.Count<T>()
            }));

        AggregateFacetResults facetResult;

        if (_fluentPipeline == null) //.Match() used
        {
            var filterDef = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);

            facetResult =
                Context.Session is not IClientSessionHandle session
                ? await Collection.Aggregate(_options).Match(filterDef).Facet(countFacet, resultsFacet).SingleAsync(cancellation).ConfigureAwait(false)
                : await Collection.Aggregate(session, _options).Match(filterDef).Facet(countFacet, resultsFacet).SingleAsync(cancellation).ConfigureAwait(false);
        }
        else //.WithFluent() used
        {
            facetResult = await _fluentPipeline
                .Facet(countFacet, resultsFacet)
                .SingleAsync(cancellation)
                .ConfigureAwait(false);
        }

        long matchCount = (
            facetResult.Facets
                       .Single(x => x.Name == "_count")
                       .Output<AggregateCountResult>().FirstOrDefault()?.Count
        ) ?? 0;

        int pageCount =
             matchCount > 0 && matchCount <= _pageSize
             ? 1
             : (int)Math.Ceiling((double)matchCount / _pageSize);

        var results = facetResult.Facets
            .First(x => x.Name == "_results")
            .Output<TProjection>();

        return (results, matchCount, pageCount);
    }
}
