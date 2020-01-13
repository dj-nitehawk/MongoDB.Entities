using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Search the text index of a collection for Entities matching the search term.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="searchTerm">The text to search the index for</param>
        /// <param name="caseSensitive">Set true to do a case sensitive search</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        /// <returns>A List of Entities of given type</returns>
        public static List<T> SearchText<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null, IClientSessionHandle session = null, string db = null)
        {
            return Run.Sync(() => SearchTextAsync(searchTerm, caseSensitive, options, session, db));
        }

        /// <summary>
        /// Search the text index of a collection for Entities matching the search term.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="searchTerm">The text to search the index for</param>
        /// <param name="caseSensitive">Set true to do a case sensitive search</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        /// <returns>A List of Entities of given type</returns>
        public List<T> SearchText<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null, IClientSessionHandle session = null)
        {
            return Run.Sync(() => SearchTextAsync(searchTerm, caseSensitive, options, session, DbName));
        }

        /// <summary>
        /// Search the text index of a collection for Entities matching the search term.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="searchTerm">The text to search the index for</param>
        /// <param name="caseSensitive">Set true to do a case sensitive search</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        /// <returns>A List of Entities of given type</returns>
        public static async Task<List<T>> SearchTextAsync<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null, IClientSessionHandle session = null, string db = null)
        {
            var filter = Builders<T>.Filter.Text(searchTerm, new TextSearchOptions { CaseSensitive = caseSensitive });
            return await (session == null
                          ? (await Collection<T>(db).FindAsync(filter, options)).ToListAsync()
                          : (await Collection<T>(db).FindAsync(session, filter, options)).ToListAsync());
        }

        /// <summary>
        /// Search the text index of a collection for Entities matching the search term.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="searchTerm">The text to search the index for</param>
        /// <param name="caseSensitive">Set true to do a case sensitive search</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        /// <returns>A List of Entities of given type</returns>
        public async Task<List<T>> SearchTextAsync<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null, IClientSessionHandle session = null)
        {
            return await SearchTextAsync(searchTerm, caseSensitive, options, session, DbName);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="searchTerm">The text to search the index for</param>
        /// <param name="caseSensitive">Set true to do a case sensitive search</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        public static IAggregateFluent<T> SearchTextFluent<T>(string searchTerm, bool caseSensitive = false, AggregateOptions options = null, IClientSessionHandle session = null, string db = null)
        {
            var filter = Builders<T>.Filter.Text(searchTerm, new TextSearchOptions { CaseSensitive = caseSensitive });
            return session == null
                   ? Collection<T>(db).Aggregate(options).Match(filter)
                   : Collection<T>(db).Aggregate(session, options).Match(filter);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="searchTerm">The text to search the index for</param>
        /// <param name="caseSensitive">Set true to do a case sensitive search</param>
        /// <param name="options">Options for finding documents (not required)</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        public IAggregateFluent<T> SearchTextFluent<T>(string searchTerm, bool caseSensitive = false, AggregateOptions options = null, IClientSessionHandle session = null)
        {
            return SearchTextFluent<T>(searchTerm, caseSensitive, options, session, DbName);
        }

    }
}
