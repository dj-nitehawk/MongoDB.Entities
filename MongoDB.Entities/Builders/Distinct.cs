using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

/// <summary>
/// Represents a MongoDB Distinct command where you can get back distinct values for a given property of a given Entity.
/// </summary>
/// <typeparam name="T">Any Entity that implements IEntity interface</typeparam>
/// <typeparam name="TProperty">The type of the property of the entity you'd like to get unique values for</typeparam>
public class Distinct<T, TProperty> where T : IEntity
{
    private FieldDefinition<T, TProperty>? field;
    private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
    private readonly DistinctOptions options = new();
    private readonly IClientSessionHandle? session;
    private readonly Dictionary<Type, (object filterDef, bool prepend)>? globalFilters;
    private bool ignoreGlobalFilters;

    internal Distinct(
        IClientSessionHandle? session,
        Dictionary<Type, (object filterDef, bool prepend)>? globalFilters)
    {
        this.session = session;
        this.globalFilters = globalFilters;
    }

    /// <summary>
    /// Specify the property you want to get the unique values for (as a string path)
    /// </summary>
    /// <param name="property">ex: "Address.Street"</param>
    public Distinct<T, TProperty> Property(string property)
    {
        field = property;
        return this;
    }

    /// <summary>
    /// Specify the property you want to get the unique values for (as a member expression)
    /// </summary>
    /// <param name="property">x => x.Address.Street</param>
    public Distinct<T, TProperty> Property(Expression<Func<T, object?>> property)
    {
        field = property.FullPath();
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    public Distinct<T, TProperty> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
    {
        this.filter &= filter(Builders<T>.Filter);
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    public Distinct<T, TProperty> Match(Expression<Func<T, bool>> expression)
    {
        return Match(f => f.Where(expression));
    }

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    public Distinct<T, TProperty> Match(Template template)
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
    public Distinct<T, TProperty> Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null)
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
    public Distinct<T, TProperty> Match(Expression<Func<T, object?>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
    {
        return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
    }

    /// <summary>
    /// Specify the matching criteria with a JSON string
    /// </summary>
    /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
    public Distinct<T, TProperty> MatchString(string jsonString)
    {
        filter &= jsonString;
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    public Distinct<T, TProperty> MatchExpression(string expression)
    {
        filter &= "{$expr:" + expression + "}";
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a Template
    /// </summary>
    /// <param name="template">A Template object</param>
    public Distinct<T, TProperty> MatchExpression(Template template)
    {
        filter &= "{$expr:" + template.RenderToString() + "}";
        return this;
    }

    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public Distinct<T, TProperty> Option(Action<DistinctOptions> option)
    {
        option(options);
        return this;
    }

    /// <summary>
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public Distinct<T, TProperty> IgnoreGlobalFilters()
    {
        ignoreGlobalFilters = true;
        return this;
    }

    /// <summary>
    /// Run the Distinct command in MongoDB server and get a cursor instead of materialized results
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<IAsyncCursor<TProperty>> ExecuteCursorAsync(CancellationToken cancellation = default)
    {
        if (field == null)
            throw new InvalidOperationException("Please use the .Property() method to specify the field to use for obtaining unique values for!");

        var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);

        return session == null
               ? DB.Collection<T>().DistinctAsync(field, mergedFilter, options, cancellation)
               : DB.Collection<T>().DistinctAsync(session, field, mergedFilter, options, cancellation);
    }

    /// <summary>
    /// Run the Distinct command in MongoDB server and get a list of unique property values
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<List<TProperty>> ExecuteAsync(CancellationToken cancellation = default)
    {
        var list = new List<TProperty>();
        using (var csr = await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
        {
            while (await csr.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                list.AddRange(csr.Current);
            }
        }
        return list;
    }
}
