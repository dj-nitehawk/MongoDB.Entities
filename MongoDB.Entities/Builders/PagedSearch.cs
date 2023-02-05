using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

/// <summary>
/// Represents an aggregation query that retrieves results with easy paging support.
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class PagedSearch<T> : PagedSearch<T, T> where T : IEntity
{
    internal PagedSearch(
        IClientSessionHandle? session,
        Dictionary<Type, (object filterDef, bool prepend)>? globalFilters)
    : base(session, globalFilters) { }
}

/// <summary>
/// Represents an aggregation query that retrieves results with easy paging support.
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
public class PagedSearch<T, TProjection> where T : IEntity
{
    private IAggregateFluent<T>? fluentPipeline;
    private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
    private readonly List<SortDefinition<T>> sorts = new();
    private readonly AggregateOptions options = new();
    private PipelineStageDefinition<T, TProjection>? projectionStage;
    private readonly IClientSessionHandle? session;
    private readonly Dictionary<Type, (object filterDef, bool prepend)>? globalFilters;
    private bool ignoreGlobalFilters;
    private int pageNumber = 1, pageSize = 100;

    internal PagedSearch(
        IClientSessionHandle? session,
        Dictionary<Type, (object filterDef, bool prepend)>? globalFilters)
    {
        var type = typeof(TProjection);
        if (type.IsPrimitive || type.IsValueType || (type == typeof(string)))
            throw new NotSupportedException("Projecting to primitive types is not supported!");

        this.session = session;
        this.globalFilters = globalFilters;
    }

    /// <summary>
    /// Begins the paged search aggregation pipeline with the provided fluent pipeline.
    /// <para>TIP: This method must be first in the chain and it cannot be used with .Match()</para>
    /// </summary>
    /// <typeparam name="TFluent">The type of the input pipeline</typeparam>
    /// <param name="fluentPipeline">The input IAggregateFluent pipeline</param>
    public PagedSearch<T, TProjection> WithFluent<TFluent>(TFluent fluentPipeline) where TFluent : IAggregateFluent<T>
    {
        this.fluentPipeline = fluentPipeline;
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
        this.filter &= filter(Builders<T>.Filter);
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a filter definition
    /// </summary>
    /// <param name="filterDefinition">A filter definition</param>
    public PagedSearch<T, TProjection> Match(FilterDefinition<T> filterDefinition)
    {
        filter &= filterDefinition;
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    public PagedSearch<T, TProjection> Match(Template template)
    {
        filter &= template.RenderToString();
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
    public PagedSearch<T, TProjection> Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null)
    {
        if (searchType == Search.Fuzzy)
        {
            searchTerm = searchTerm.ToDoubleMetaphoneHash();
            caseSensitive = false;
            diacriticSensitive = false;
            language = null;
        }

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
    public PagedSearch<T, TProjection> Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
    {
        return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
    }

    /// <summary>
    /// Specify the matching criteria with a JSON string
    /// </summary>
    /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
    public PagedSearch<T, TProjection> MatchString(string jsonString)
    {
        filter &= jsonString;
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    public PagedSearch<T, TProjection> MatchExpression(string expression)
    {
        filter &= "{$expr:" + expression + "}";
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a Template
    /// </summary>
    /// <param name="template">A Template object</param>
    public PagedSearch<T, TProjection> MatchExpression(Template template)
    {
        filter &= "{$expr:" + template.RenderToString() + "}";
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
            _ => this,
        };
    }

    /// <summary>
    /// Sort the results of a text search by the MetaTextScore
    /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
    /// </summary>
    public PagedSearch<T, TProjection> SortByTextScore()
    {
        return SortByTextScore(null);
    }

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

    private void AddTxtScoreToProjection(string fieldName)
    {
        if (projectionStage == null)
        {
            projectionStage = $"{{ $set : {{ {fieldName} : {{ $meta : 'textScore' }}  }} }}";
            return;
        }

        var renderedStage = projectionStage.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry,
            Driver.Linq.LinqProvider.V3);

        renderedStage.Document["$project"][fieldName] = new BsonDocument { { "$meta", "textScore" } };

        projectionStage = renderedStage.Document;
    }

    /// <summary>
    /// Specify how to sort using a sort expression
    /// </summary>
    /// <param name="sortFunction">s => s.Ascending("Prop1").MetaTextScore("Prop2")</param>
    public PagedSearch<T, TProjection> Sort(Func<SortDefinitionBuilder<T>, SortDefinition<T>> sortFunction)
    {
        sorts.Add(sortFunction(Builders<T>.Sort));
        return this;
    }

    /// <summary>
    /// Specify the page number to get
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    public PagedSearch<T, TProjection> PageNumber(int pageNumber)
    {
        this.pageNumber = pageNumber;
        return this;
    }

    /// <summary>
    /// Specify the number of items per page
    /// </summary>
    /// <param name="pageSize">The size of a page</param>
    public PagedSearch<T, TProjection> PageSize(int pageSize)
    {
        this.pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Specify how to project the results using a lambda expression
    /// </summary>
    /// <param name="expression">x => new Test { PropName = x.Prop }</param>
    public PagedSearch<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
    {
        projectionStage = PipelineStageDefinitionBuilder.Project(expression);
        return this;
    }

    /// <summary>
    /// Specify how to project the results using a projection expression
    /// </summary>
    /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
    public PagedSearch<T, TProjection> Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
    {
        projectionStage = PipelineStageDefinitionBuilder.Project(projection(Builders<T>.Projection));
        return this;
    }

    /// <summary>
    /// Specify how to project the results using an exclusion projection expression.
    /// </summary>
    /// <param name="exclusion">x => new { x.PropToExclude, x.AnotherPropToExclude }</param>
    public PagedSearch<T, TProjection> ProjectExcluding(Expression<Func<T, object>> exclusion)
    {
        var props = (exclusion.Body as NewExpression)?.Arguments
            .Select(a => a.ToString().Split('.')[1]);

        if (props?.Any() != true)
            throw new ArgumentException("Unable to get any properties from the exclusion expression!");

        var defs = new List<ProjectionDefinition<T>>(props.Count());

        foreach (var prop in props)
        {
            defs.Add(Builders<T>.Projection.Exclude(prop));
        }

        projectionStage = PipelineStageDefinitionBuilder.Project<T, TProjection>(Builders<T>.Projection.Combine(defs));

        return this;
    }

    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public PagedSearch<T, TProjection> Option(Action<AggregateOptions> option)
    {
        option(options);
        return this;
    }

    /// <summary>
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public PagedSearch<T, TProjection> IgnoreGlobalFilters()
    {
        ignoreGlobalFilters = true;
        return this;
    }

    /// <summary>
    /// Run the aggregation search command in MongoDB server and get a page of results and total + page count
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<(IReadOnlyList<TProjection> Results, long TotalCount, int PageCount)> ExecuteAsync(CancellationToken cancellation = default)
    {
        if (filter != Builders<T>.Filter.Empty && fluentPipeline != null)
            throw new InvalidOperationException(".Match() and .WithFluent() cannot be used together!");

        var pipelineStages = new List<IPipelineStageDefinition>(4);

        if (sorts.Count == 0)
            throw new InvalidOperationException("Paging without sorting is a sin!");
        else
            pipelineStages.Add(PipelineStageDefinitionBuilder.Sort(Builders<T>.Sort.Combine(sorts)));

        pipelineStages.Add(PipelineStageDefinitionBuilder.Skip<T>((pageNumber - 1) * pageSize));
        pipelineStages.Add(PipelineStageDefinitionBuilder.Limit<T>(pageSize));

        if (projectionStage != null)
            pipelineStages.Add(projectionStage);

        var resultsFacet = AggregateFacet.Create<T, TProjection>("_results", pipelineStages);

        var countFacet = AggregateFacet.Create("_count",
            PipelineDefinition<T, AggregateCountResult>.Create(new[]
            {
                PipelineStageDefinitionBuilder.Count<T>()
            }));

        AggregateFacetResults facetResult;

        if (fluentPipeline == null) //.Match() used
        {
            var filterDef = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);

            facetResult =
                session == null
                ? await DB.Collection<T>().Aggregate(options).Match(filterDef).Facet(countFacet, resultsFacet).SingleAsync(cancellation).ConfigureAwait(false)
                : await DB.Collection<T>().Aggregate(session, options).Match(filterDef).Facet(countFacet, resultsFacet).SingleAsync(cancellation).ConfigureAwait(false);
        }
        else //.WithFluent() used
        {
            facetResult = await fluentPipeline
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
             matchCount > 0 && matchCount <= pageSize
             ? 1
             : (int)Math.Ceiling((double)matchCount / pageSize);

        var results = facetResult.Facets
            .First(x => x.Name == "_results")
            .Output<TProjection>();

        return (results, matchCount, pageCount);
    }
}
