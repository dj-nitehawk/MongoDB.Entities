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
/// Represents a MongoDB Find command.
/// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
/// <para>Note: For building queries, use the DB.Fluent* interfaces</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class Find<T> : Find<T, T> where T : IEntity
{
    internal Find(IClientSessionHandle? session, Dictionary<Type, (object filterDef, bool prepend)>? globalFilters)
        : base(session, globalFilters) { }
}

/// <summary>
/// Represents a MongoDB Find command with the ability to project to a different result type.
/// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
public class Find<T, TProjection> where T : IEntity
{
    FilterDefinition<T> filter = Builders<T>.Filter.Empty;
    readonly List<SortDefinition<T>> sorts = new();
    readonly FindOptions<T, TProjection> options = new();
    readonly IClientSessionHandle? session;
    readonly Dictionary<Type, (object filterDef, bool prepend)>? globalFilters;
    bool ignoreGlobalFilters;

    internal Find(IClientSessionHandle? session, Dictionary<Type, (object filterDef, bool prepend)>? globalFilters)
    {
        this.session = session;
        this.globalFilters = globalFilters;
    }

    /// <summary>
    /// Find a single IEntity by ID
    /// </summary>
    /// <param name="ID">The unique ID of an IEntity</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A single entity or null if not found</returns>
    public Task<TProjection?> OneAsync(object ID, CancellationToken cancellation = default)
    {
        Match(ID);
        return ExecuteSingleAsync(cancellation);
    }

    /// <summary>
    /// Find entities by supplying a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A list of Entities</returns>
    public Task<List<TProjection>> ManyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellation = default)
    {
        Match(expression);
        return ExecuteAsync(cancellation);
    }

    /// <summary>
    /// Find entities by supplying a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A list of Entities</returns>
    public Task<List<TProjection>> ManyAsync(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default)
    {
        Match(filter);
        return ExecuteAsync(cancellation);
    }

    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="ID">A unique IEntity ID</param>
    public Find<T, TProjection> MatchID(object ID)
    {
        return Match(f => f.Eq(Cache<T>.IdPropName, ID));
    }

    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="ID">A unique IEntity ID</param>
    public Find<T, TProjection> Match(object ID)
    {
        return Match(f => f.Eq(Cache<T>.IdPropName, ID));
    }

    /// <summary>
    /// Specify the matching criteria with a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    public Find<T, TProjection> Match(Expression<Func<T, bool>> expression)
    {
        return Match(f => f.Where(expression));
    }

    /// <summary>
    /// Specify the matching criteria with a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    public Find<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
    {
        this.filter &= filter(Builders<T>.Filter);
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a filter definition
    /// </summary>
    /// <param name="filterDefinition">A filter definition</param>
    public Find<T, TProjection> Match(FilterDefinition<T> filterDefinition)
    {
        filter &= filterDefinition;
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    public Find<T, TProjection> Match(Template template)
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
    public Find<T, TProjection> Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null)
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
    public Find<T, TProjection> Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
    {
        return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
    }

    /// <summary>
    /// Specify the matching criteria with a JSON string
    /// </summary>
    /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
    public Find<T, TProjection> MatchString(string jsonString)
    {
        filter &= jsonString;
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    public Find<T, TProjection> MatchExpression(string expression)
    {
        filter &= "{$expr:" + expression + "}";
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a Template
    /// </summary>
    /// <param name="template">A Template object</param>
    public Find<T, TProjection> MatchExpression(Template template)
    {
        filter &= "{$expr:" + template.RenderToString() + "}";
        return this;
    }

    /// <summary>
    /// Specify which property and order to use for sorting (use multiple times if needed)
    /// </summary>
    /// <param name="propertyToSortBy">x => x.Prop</param>
    /// <param name="sortOrder">The sort order</param>
    public Find<T, TProjection> Sort(Expression<Func<T, object>> propertyToSortBy, Order sortOrder)
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
    public Find<T, TProjection> SortByTextScore()
    {
        return SortByTextScore(null);
    }

    /// <summary>
    /// Sort the results of a text search by the MetaTextScore and get back the score as well
    /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
    /// </summary>
    /// <param name="scoreProperty">x => x.TextScoreProp</param>
    public Find<T, TProjection> SortByTextScore(Expression<Func<T, object>>? scoreProperty)
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

    /// <summary>
    /// Specify how to sort using a sort expression
    /// </summary>
    /// <param name="sortFunction">s => s.Ascending("Prop1").MetaTextScore("Prop2")</param>
    /// <returns></returns>
    public Find<T, TProjection> Sort(Func<SortDefinitionBuilder<T>, SortDefinition<T>> sortFunction)
    {
        sorts.Add(sortFunction(Builders<T>.Sort));
        return this;
    }

    /// <summary>
    /// Specify how many entities to skip
    /// </summary>
    /// <param name="skipCount">The number to skip</param>
    public Find<T, TProjection> Skip(int skipCount)
    {
        options.Skip = skipCount;
        return this;
    }

    /// <summary>
    /// Specify how many entities to Take/Limit
    /// </summary>
    /// <param name="takeCount">The number to limit/take</param>
    public Find<T, TProjection> Limit(int takeCount)
    {
        options.Limit = takeCount;
        return this;
    }

    /// <summary>
    /// Specify how to project the results using a lambda expression
    /// </summary>
    /// <param name="expression">x => new Test { PropName = x.Prop }</param>
    public Find<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
    {
        return Project(p => p.Expression(expression));
    }

    /// <summary>
    /// Specify how to project the results using a projection expression
    /// </summary>
    /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
    public Find<T, TProjection> Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
    {
        options.Projection = projection(Builders<T>.Projection)!;
        return this;
    }

    /// <summary>
    /// Specify how to project the results using an exclusion projection expression.
    /// </summary>
    /// <param name="exclusion">x => new { x.PropToExclude, x.AnotherPropToExclude }</param>
    public Find<T, TProjection> ProjectExcluding(Expression<Func<T, object>> exclusion)
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

        options.Projection = Builders<T>.Projection.Combine(defs);

        return this;
    }

    /// <summary>
    /// Specify to automatically include all properties marked with [BsonRequired] attribute on the entity in the final projection.
    /// <para>HINT: this method should only be called after the .Project() method.</para>
    /// </summary>
    public Find<T, TProjection> IncludeRequiredProps()
    {
        if (typeof(T) != typeof(TProjection))
            throw new InvalidOperationException("IncludeRequiredProps() cannot be used when projecting to a different type.");

        options.Projection = Cache<T>.CombineWithRequiredProps(options.Projection);
        return this;
    }

    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public Find<T, TProjection> Option(Action<FindOptions<T, TProjection>> option)
    {
        option(options);
        return this;
    }

    /// <summary>
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public Find<T, TProjection> IgnoreGlobalFilters()
    {
        ignoreGlobalFilters = true;
        return this;
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get a list of results
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<List<TProjection>> ExecuteAsync(CancellationToken cancellation = default)
    {
        var list = new List<TProjection>();
        using (var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
        {
            while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                list.AddRange(cursor.Current);
            }
        }
        return list;
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get a single result or the default value if not found.
    /// If more than one entity is found, it will throw an exception.
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<TProjection?> ExecuteSingleAsync(CancellationToken cancellation = default)
    {
        Limit(2);
        using var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
        return cursor.Current.SingleOrDefault();
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get the first result or the default value if not found
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<TProjection?> ExecuteFirstAsync(CancellationToken cancellation = default)
    {
        Limit(1);
        using var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
        return cursor.Current.SingleOrDefault(); //because we're limiting to 1
    }

    /// <summary>
    /// Run the Find command and get back a bool indicating whether any entities matched the query
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<bool> ExecuteAnyAsync(CancellationToken cancellation = default)
    {
        Project(b => b.Include(Cache<T>.IdExpression));
        Limit(1);
        using var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
        return cursor.Current.Any();
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get a cursor instead of materialized results
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<IAsyncCursor<TProjection>> ExecuteCursorAsync(CancellationToken cancellation = default)
    {
        if (sorts.Count > 0)
            options.Sort = Builders<T>.Sort.Combine(sorts);

        var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);

        return session == null
               ? DB.Collection<T>().FindAsync(mergedFilter, options, cancellation)
               : DB.Collection<T>().FindAsync(session, mergedFilter, options, cancellation);
    }

    void AddTxtScoreToProjection(string propName)
    {
        options.Projection ??= "{}";

        options.Projection =
            options.Projection
            .Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry, Driver.Linq.LinqProvider.V3)
            .Document.Add(propName, new BsonDocument { { "$meta", "textScore" } });
    }
}

public enum Order
{
    Ascending,
    Descending
}

public enum Search
{
    Fuzzy,
    Full
}
