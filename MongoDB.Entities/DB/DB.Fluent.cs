using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Exposes the MongoDB collection for the given entity type as IAggregateFluent in order to facilitate Fluent queries
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="options">The options for the aggregation. This is not required.</param>
    public IAggregateFluent<T> Fluent<T>(AggregateOptions? options = null) where T : IEntity
    {
        var globalFilter = Logic.MergeWithGlobalFilter(IgnoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);
        var fluent = Session == null
                         ? Collection<T>().Aggregate(options)
                         : Collection<T>().Aggregate(Session, options);

        return globalFilter != Builders<T>.Filter.Empty
                   ? fluent.Match(globalFilter)
                   : fluent;
    }

    /// <summary>
    /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters
    /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
    /// </summary>
    /// <param name="searchType">The type of text matching to do</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
    /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
    /// <param name="language">The language for the search (optional)</param>
    /// <param name="options">Options for finding documents (not required)</param>
    public IAggregateFluent<T> FluentTextSearch<T>(Search searchType,
                                                   string searchTerm,
                                                   bool caseSensitive = false,
                                                   bool diacriticSensitive = false,
                                                   string? language = null,
                                                   AggregateOptions? options = null) where T : IEntity
    {
        var globalFilter = Logic.MergeWithGlobalFilter(IgnoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);

        if (searchType == Search.Fuzzy)
        {
            searchTerm = searchTerm.ToDoubleMetaphoneHash();
            caseSensitive = false;
            diacriticSensitive = false;
            language = null;
        }

        var filter = Builders<T>.Filter.Text(
            searchTerm,
            new TextSearchOptions
            {
                CaseSensitive = caseSensitive,
                DiacriticSensitive = diacriticSensitive,
                Language = language
            });

        var fluent = Session == null
                         ? Collection<T>().Aggregate(options).Match(filter)
                         : Collection<T>().Aggregate(Session, options).Match(filter);

        return globalFilter != Builders<T>.Filter.Empty
                   ? fluent.Match(globalFilter)
                   : fluent;
    }
}