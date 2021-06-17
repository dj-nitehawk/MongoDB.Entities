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
        public IAggregateFluent<T> Fluent<T>(AggregateOptions options = null, bool ignoreGlobalFilters = false) where T : IEntity
        {
            var globalFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, Builders<T>.Filter.Empty);

            if (globalFilter != Builders<T>.Filter.Empty)
            {
                return DB
                    .Fluent<T>(options, Session)
                    .Match(globalFilter);
            }

            return DB.Fluent<T>(options, Session);
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
        public IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null, AggregateOptions options = null, bool ignoreGlobalFilters = false) where T : IEntity
        {
            var globalFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, Builders<T>.Filter.Empty);

            if (globalFilter != Builders<T>.Filter.Empty)
            {
                return DB
                    .FluentTextSearch<T>(searchType, searchTerm, caseSensitive, diacriticSensitive, language, options, Session)
                    .Match(globalFilter);
            }

            return DB.FluentTextSearch<T>(searchType, searchTerm, caseSensitive, diacriticSensitive, language, options, Session);
        }
    }
}
