using MongoDB.Driver;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IAggregateFluent in order to facilitate Fluent queries.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static IAggregateFluent<T> Fluent<T>(string tenantPrefix, AggregateOptions options = null, IClientSessionHandle session = null) where T : IEntity
        {
            return session == null
                   ? Collection<T>(tenantPrefix).Aggregate(options)
                   : Collection<T>(tenantPrefix).Aggregate(session, options);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="searchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm, string tenantPrefix, bool caseSensitive = false, bool diacriticSensitive = false, string language = null, AggregateOptions options = null, IClientSessionHandle session = null) where T : IEntity
        {
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

            return session == null
                   ? Collection<T>(tenantPrefix).Aggregate(options).Match(filter)
                   : Collection<T>(tenantPrefix).Aggregate(session, options).Match(filter);
        }
    }
}
