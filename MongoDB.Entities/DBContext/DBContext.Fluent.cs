using MongoDB.Driver;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IAggregateFluent in order to facilitate Fluent queries
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public IAggregateFluent<T> Fluent<T>(AggregateOptions? options = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            var globalFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);

            var aggregate = Session is not IClientSessionHandle session
                   ? Collection(collectionName, collection).Aggregate(options)
                   : Collection(collectionName, collection).Aggregate(session, options);

            if (globalFilter != Builders<T>.Filter.Empty)
            {
                aggregate = aggregate.Match(globalFilter);
            }
            return aggregate;
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
        /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null, AggregateOptions? options = null, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            var globalFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);

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

            var aggregate = Session is not IClientSessionHandle session
                   ? Collection(collectionName, collection).Aggregate(options).Match(filter)
                   : Collection(collectionName, collection).Aggregate(session, options).Match(filter);


            if (globalFilter != Builders<T>.Filter.Empty)
            {
                aggregate = aggregate.Match(globalFilter);
            }

            return aggregate;
        }
    }
}
