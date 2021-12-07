namespace MongoDB.Entities;


public abstract class FilterQueryBase<T, TSelf> : IFilterBuilder<T, TSelf>
    where TSelf : FilterQueryBase<T, TSelf>

{
    internal FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
    internal Dictionary<Type, (object filterDef, bool prepend)> _globalFilters;
    internal bool _ignoreGlobalFilters;

    internal FilterQueryBase(IFilterBuilder<T, TSelf> other) : this(globalFilters: other.GlobalFilters)
    {
        _filter = other.Filter;
        _ignoreGlobalFilters = other.IsIgnoreGlobalFilters;
    }

    internal FilterQueryBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters)
    {
        _globalFilters = globalFilters;
    }

    bool IFilterBuilder<T, TSelf>.IsIgnoreGlobalFilters => _ignoreGlobalFilters;
    FilterDefinition<T> IFilterBuilder<T, TSelf>.Filter => _filter;
    Dictionary<Type, (object filterDef, bool prepend)> IFilterBuilder<T, TSelf>.GlobalFilters => _globalFilters;

    private TSelf This => (TSelf)this;

    /// <summary>
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public TSelf IgnoreGlobalFilters(bool IgnoreGlobalFilters = true)
    {
        _ignoreGlobalFilters = IgnoreGlobalFilters;
        return This;
    }

    public TSelf Match(Expression<Func<T, bool>> expression)
    {
        return Match(f => f.Where(expression));
    }

    public TSelf Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
    {
        _filter &= filter(Builders<T>.Filter);
        return This;
    }

    public TSelf Match(FilterDefinition<T> filterDefinition)
    {
        _filter &= filterDefinition;
        return This;
    }

    public TSelf Match(Template template)
    {
        _filter &= template.RenderToString();
        return This;
    }

    public TSelf Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null)
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

    public TSelf Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
    {
        return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
    }

    public TSelf MatchExpression(string expression)
    {
        _filter &= "{$expr:" + expression + "}";
        return This;
    }

    public TSelf MatchExpression(Template template)
    {
        return MatchExpression(template.RenderToString());
    }


    public TSelf MatchString(string jsonString)
    {
        _filter &= jsonString;
        return This;
    }
}
