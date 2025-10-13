using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Represents an aggregation query that retrieves results with easy paging support.
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class PagedSearch<T> : PagedSearch<T, T> where T : IEntity
{
    internal PagedSearch(IClientSessionHandle? session, Dictionary<Type, (object filterDef, bool prepend)>? globalFilters, DBInstance dbInstance)
        : base(session, globalFilters, dbInstance) { }
}

/// <summary>
/// Represents an aggregation query that retrieves results with easy paging support.
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
public class PagedSearch<T, TProjection> where T : IEntity
{
    IAggregateFluent<T>? _fluentPipeline;
    FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
    readonly List<SortDefinition<T>> _sorts = [];
    readonly AggregateOptions _options = new();
    PipelineStageDefinition<T, TProjection>? _projectionStage;
    readonly IClientSessionHandle? _session;
    readonly Dictionary<Type, (object filterDef, bool prepend)>? _globalFilters;
    readonly DBInstance _dbInstance;
    
    bool _ignoreGlobalFilters;

    int _pageNumber = 1,
        _pageSize = 100;

    internal PagedSearch(IClientSessionHandle? session, Dictionary<Type, (object filterDef, bool prepend)>? globalFilters, DBInstance dbInstance)
    {
        var type = typeof(TProjection);

        if (type.IsPrimitive || type.IsValueType || type == typeof(string))
            throw new NotSupportedException("Projecting to primitive types is not supported!");

        _session = session;
        _globalFilters = globalFilters;
        _dbInstance = dbInstance;
    }

    /// <summary>
    /// Begins the paged search aggregation pipeline with the provided fluent pipeline.
    /// <para>TIP: This method must be first in the chain and it cannot be used with .Match()</para>
    /// </summary>
    /// <typeparam name="TFluent">The type of the input pipeline</typeparam>
    /// <param name="fluentPipeline">The input IAggregateFluent pipeline</param>
    public PagedSearch<T, TProjection> WithFluent<TFluent>(TFluent fluentPipeline) where TFluent : IAggregateFluent<T>
    {
        _fluentPipeline = fluentPipeline;

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    public PagedSearch<T, TProjection> Match(Expression<Func<T, bool>> expression)
    {
        return Match(f => f.Where(expression));
    }

    /// <summary>
    /// Specify the matching criteria with a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    public PagedSearch<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
    {
        _filter &= filter(Builders<T>.Filter);

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a filter definition
    /// </summary>
    /// <param name="filterDefinition">A filter definition</param>
    public PagedSearch<T, TProjection> Match(FilterDefinition<T> filterDefinition)
    {
        _filter &= filterDefinition;

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    public PagedSearch<T, TProjection> Match(Template template)
    {
        _filter &= template.RenderToString();

        return this;
    }

    /// <summary>
    /// Specify a search term to find results from the text index of this particular collection.
    /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
    /// </summary>
    /// <param name="searchType">The type of text matching to do</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
    /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
    /// <param name="language">The language for the search (optional)</param>
    public PagedSearch<T, TProjection> Match(Search searchType,
                                             string searchTerm,
                                             bool caseSensitive = false,
                                             bool diacriticSensitive = false,
                                             string? language = null)
    {
        if (searchType != Search.Fuzzy)
        {
            return Match(
                f => f.Text(
                    searchTerm,
                    new TextSearchOptions
                    {
                        CaseSensitive = caseSensitive,
                        DiacriticSensitive = diacriticSensitive,
                        Language = language
                    }));
        }

        searchTerm = searchTerm.ToDoubleMetaphoneHash();
        caseSensitive = false;
        diacriticSensitive = false;
        language = null;

        return Match(
            f => f.Text(
                searchTerm,
                new TextSearchOptions
                {
                    CaseSensitive = caseSensitive,
                    DiacriticSensitive = diacriticSensitive,
                    Language = language
                }));
    }

    /// <summary>
    /// Specify criteria for matching entities based on GeoSpatial data (longitude &amp; latitude)
    /// <para>TIP: Make sure to define a Geo2DSphere index with DB.Index&lt;T&gt;() before searching</para>
    /// <para>Note: DB.FluentGeoNear() supports more advanced options</para>
    /// </summary>
    /// <param name="coordinatesProperty">The property where 2DCoordinates are stored</param>
    /// <param name="nearCoordinates">The search point</param>
    /// <param name="maxDistance">Maximum distance in meters from the search point</param>
    /// <param name="minDistance">Minimum distance in meters from the search point</param>
    public PagedSearch<T, TProjection> Match(Expression<Func<T, object?>> coordinatesProperty,
                                             Coordinates2D nearCoordinates,
                                             double? maxDistance = null,
                                             double? minDistance = null)
    {
        return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
    }

    /// <summary>
    /// Specify the matching criteria with a JSON string
    /// </summary>
    /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
    public PagedSearch<T, TProjection> MatchString(string jsonString)
    {
        _filter &= jsonString;

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    public PagedSearch<T, TProjection> MatchExpression(string expression)
    {
        _filter &= "{$expr:" + expression + "}";

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a Template
    /// </summary>
    /// <param name="template">A Template object</param>
    public PagedSearch<T, TProjection> MatchExpression(Template template)
    {
        _filter &= "{$expr:" + template.RenderToString() + "}";

        return this;
    }

    /// <summary>
    /// Specify which property and order to use for sorting (use multiple times if needed)
    /// </summary>
    /// <param name="propertyToSortBy">x => x.Prop</param>
    /// <param name="sortOrder">The sort order</param>
    public PagedSearch<T, TProjection> Sort(Expression<Func<T, object?>> propertyToSortBy, Order sortOrder)
    {
        return sortOrder switch
        {
            Order.Ascending => Sort(s => s.Ascending(propertyToSortBy)),
            Order.Descending => Sort(s => s.Descending(propertyToSortBy)),
            _ => this
        };
    }

    /// <summary>
    /// Sort the results of a text search by the MetaTextScore
    /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
    /// </summary>
    public PagedSearch<T, TProjection> SortByTextScore()
        => SortByTextScore(null);

    /// <summary>
    /// Sort the results of a text search by the MetaTextScore and get back the score as well
    /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
    /// </summary>
    /// <param name="scoreProperty">x => x.TextScoreProp</param>
    public PagedSearch<T, TProjection> SortByTextScore(Expression<Func<T, object?>>? scoreProperty)
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

    void AddTxtScoreToProjection(string fieldName)
    {
        if (_projectionStage == null)
        {
            _projectionStage = $"{{ $set : {{ {fieldName} : {{ $meta : 'textScore' }}  }} }}";

            return;
        }

        var renderedStage = _projectionStage.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry));

        renderedStage.Document["$project"][fieldName] = new BsonDocument { { "$meta", "textScore" } };

        _projectionStage = renderedStage.Document;
    }

    /// <summary>
    /// Specify how to sort using a sort expression
    /// </summary>
    /// <param name="sortFunction">s => s.Ascending("Prop1").MetaTextScore("Prop2")</param>
    public PagedSearch<T, TProjection> Sort(Func<SortDefinitionBuilder<T>, SortDefinition<T>> sortFunction)
    {
        _sorts.Add(sortFunction(Builders<T>.Sort));

        return this;
    }

    /// <summary>
    /// Specify the page number to get
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    public PagedSearch<T, TProjection> PageNumber(int pageNumber)
    {
        _pageNumber = pageNumber;

        return this;
    }

    /// <summary>
    /// Specify the number of items per page
    /// </summary>
    /// <param name="pageSize">The size of a page</param>
    public PagedSearch<T, TProjection> PageSize(int pageSize)
    {
        _pageSize = pageSize;

        return this;
    }

    /// <summary>
    /// Specify how to project the results using a lambda expression
    /// </summary>
    /// <param name="expression">x => new Test { PropName = x.Prop }</param>
    public PagedSearch<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
    {
        _projectionStage = PipelineStageDefinitionBuilder.Project(expression);

        return this;
    }

    /// <summary>
    /// Specify how to project the results using a projection expression
    /// </summary>
    /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
    public PagedSearch<T, TProjection> Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
    {
        _projectionStage = PipelineStageDefinitionBuilder.Project(projection(Builders<T>.Projection));

        return this;
    }

    /// <summary>
    /// Specify how to project the results using an exclusion projection expression.
    /// </summary>
    /// <param name="exclusion">x => new { x.PropToExclude, x.AnotherPropToExclude }</param>
    public PagedSearch<T, TProjection> ProjectExcluding(Expression<Func<T, object?>> exclusion)
    {
        var props = (exclusion.Body as NewExpression)?.Arguments
                                                     .Select(a => a.ToString().Split('.')[1]);

        if (props?.Any() != true)
            throw new ArgumentException("Unable to get any properties from the exclusion expression!");

        var defs = new List<ProjectionDefinition<T>>(props.Count());
        defs.AddRange(props.Select(prop => Builders<T>.Projection.Exclude(prop)));

        _projectionStage = PipelineStageDefinitionBuilder.Project<T, TProjection>(Builders<T>.Projection.Combine(defs));

        return this;
    }

    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public PagedSearch<T, TProjection> Option(Action<AggregateOptions> option)
    {
        option(_options);

        return this;
    }

    /// <summary>
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public PagedSearch<T, TProjection> IgnoreGlobalFilters()
    {
        _ignoreGlobalFilters = true;

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

        pipelineStages.Add(PipelineStageDefinitionBuilder.Sort(Builders<T>.Sort.Combine(_sorts)));
        pipelineStages.Add(PipelineStageDefinitionBuilder.Skip<T>((_pageNumber - 1) * _pageSize));
        pipelineStages.Add(PipelineStageDefinitionBuilder.Limit<T>(_pageSize));

        if (_projectionStage != null)
            pipelineStages.Add(_projectionStage);

        var resultsFacet = AggregateFacet.Create<T, TProjection>("_results", pipelineStages);
        var countFacet = AggregateFacet.Create("_count", PipelineDefinition<T, AggregateCountResult>.Create([PipelineStageDefinitionBuilder.Count<T>()]));

        AggregateFacetResults facetResult;

        if (_fluentPipeline == null) //.Match() used
        {
            var filterDef = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);

            facetResult =
                _session == null
                    ? await _dbInstance.Collection<T>().Aggregate(_options).Match(filterDef).Facet(countFacet, resultsFacet).SingleAsync(cancellation)
                              .ConfigureAwait(false)
                    : await _dbInstance.Collection<T>().Aggregate(_session, _options).Match(filterDef).Facet(countFacet, resultsFacet).SingleAsync(cancellation)
                              .ConfigureAwait(false);
        }
        else //.WithFluent() used
        {
            facetResult = await _fluentPipeline
                                .Facet(countFacet, resultsFacet)
                                .SingleAsync(cancellation)
                                .ConfigureAwait(false);
        }

        var matchCount = facetResult.Facets
                                    .Single(x => x.Name == "_count")
                                    .Output<AggregateCountResult>().FirstOrDefault()?.Count ??
                         0;

        var pageCount =
            matchCount > 0 && matchCount <= _pageSize
                ? 1
                : (int)Math.Ceiling((double)matchCount / _pageSize);

        var results = facetResult.Facets
                                 .First(x => x.Name == "_results")
                                 .Output<TProjection>();

        return (results, matchCount, pageCount);
    }
}