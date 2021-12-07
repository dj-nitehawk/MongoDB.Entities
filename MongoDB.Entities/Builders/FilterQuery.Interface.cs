namespace MongoDB.Entities;

public interface IFilterBuilder<T, TSelf>
    where TSelf : IFilterBuilder<T, TSelf>
{
    internal bool IsIgnoreGlobalFilters { get; }
    internal FilterDefinition<T> Filter { get; }
    internal Dictionary<Type, (object filterDef, bool prepend)> GlobalFilters { get; }

    /// <summary>
    /// Specify that this operation should ignore any global filters
    /// </summary>
    TSelf IgnoreGlobalFilters(bool IgnoreGlobalFilters = true);

    /// <summary>
    /// Specify the matching criteria with a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    TSelf Match(Expression<Func<T, bool>> expression);

    /// <summary>
    /// Specify the matching criteria with a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    TSelf Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter);

    /// <summary>
    /// Specify the matching criteria with a filter definition
    /// </summary>
    /// <param name="filterDefinition">A filter definition</param>
    TSelf Match(FilterDefinition<T> filterDefinition);

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    TSelf Match(Template template);

    /// <summary>
    /// Specify a search term to find results from the text index of this particular collection.
    /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
    /// </summary>
    /// <param name="searchType">The type of text matching to do</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
    /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
    /// <param name="language">The language for the search (optional)</param>
    TSelf Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null);

    /// <summary>
    /// Specify criteria for matching entities based on GeoSpatial data (longitude &amp; latitude)
    /// <para>TIP: Make sure to define a Geo2DSphere index with DB.Index&lt;T&gt;() before searching</para>
    /// <para>Note: DB.FluentGeoNear() supports more advanced options</para>
    /// </summary>
    /// <param name="coordinatesProperty">The property where 2DCoordinates are stored</param>
    /// <param name="nearCoordinates">The search point</param>
    /// <param name="maxDistance">Maximum distance in meters from the search point</param>
    /// <param name="minDistance">Minimum distance in meters from the search point</param>
    TSelf Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null);

    /// <summary>
    /// Specify the matching criteria with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    TSelf MatchExpression(string expression);

    /// <summary>
    /// Specify the matching criteria with a Template
    /// </summary>
    /// <param name="template">A Template object</param>
    TSelf MatchExpression(Template template);

    /// <summary>
    /// Specify the matching criteria with a JSON string
    /// </summary>
    /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
    TSelf MatchString(string jsonString);
}
