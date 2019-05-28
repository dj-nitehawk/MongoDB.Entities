using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a MongoDB Find command
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that inherits from Entity</typeparam>
    public class Find<T> : Find<T, T> where T : Entity { }

    /// <summary>
    /// Represents a MongoDB Find command
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that inherits from Entity</typeparam>
    /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
    public class Find<T, TProjection> where T : Entity
    {
        private FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
        private List<SortDefinition<T>> _sorts = new List<SortDefinition<T>>();
        private FindOptions<T, TProjection> _options = new FindOptions<T, TProjection>();

        /// <summary>
        /// Find a single Entity by ID
        /// </summary>
        /// <param name="ID">The unique ID of an Entity</param>
        /// <returns>A single entity or null if not found</returns>
        public TProjection By(string ID)
        {
            return ByAsync(ID).GetAwaiter().GetResult();

        }

        /// <summary>
        /// Find a single Entity by ID
        /// </summary>
        /// <param name="ID">The unique ID of an Entity</param>
        /// <returns>A single entity or null if not found</returns>
        async public Task<TProjection> ByAsync(string ID)
        {
            Match(ID);
            return (await ExecuteAsync()).SingleOrDefault();
        }

        /// <summary>
        /// Find entities by supplying a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        /// <returns>A list of Entities</returns>
        public List<TProjection> By(Expression<Func<T, bool>> expression)
        {
            return ByAsync(expression).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Find entities by supplying a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        /// <returns>A list of Entities</returns>
        async public Task<List<TProjection>> ByAsync(Expression<Func<T, bool>> expression)
        {
            Match(expression);
            return await ExecuteAsync();
        }

        public List<TProjection> By(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            return ByAsync(filter).GetAwaiter().GetResult();
        }

        async public Task<List<TProjection>> ByAsync(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            Match(filter);
            return await ExecuteAsync();
        }

        /// <summary>
        /// Specify an Entity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique Entity ID</param>
        public Find<T, TProjection> Match(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
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
        /// Specify the matching criteria with MongoDB filters
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Find<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            _filter = filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify which property and order to use for sorting (use multiple times if needed)
        /// </summary>
        /// <param name="propertyToSortBy">x => x.Prop</param>
        /// <param name="sortOrder">The sort order</param>
        public Find<T, TProjection> Sort(Expression<Func<T, object>> propertyToSortBy, Order sortOrder)
        {
            switch (sortOrder)
            {
                case Order.Ascending:
                    _sorts.Add(Builders<T>.Sort.Ascending(propertyToSortBy));
                    break;
                case Order.Descending:
                    _sorts.Add(Builders<T>.Sort.Descending(propertyToSortBy));
                    break;
            }

            return this;
        }

        /// <summary>
        /// Specify how many entities to skip
        /// </summary>
        /// <param name="skipCount">The number to skip</param>
        public Find<T, TProjection> Skip(int skipCount)
        {
            _options.Skip = skipCount;
            return this;
        }

        /// <summary>
        /// Specify how many entiteis to Take/Limit
        /// </summary>
        /// <param name="takeCount">The number to limit/take</param>
        public Find<T, TProjection> Take(int takeCount)
        {
            _options.Limit = takeCount;
            return this;
        }

        /// <summary>
        /// Specify how to project the results using a lambda expression
        /// </summary>
        /// <param name="expression">x => new Test { PropName = x.Prop }</param>
        public Find<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
        {
            _options.Projection = Builders<T>.Projection.Expression(expression);
            return this;
        }

        /// <summary>
        /// Specify an option for this find command (use multiple times if needed)
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Find<T, TProjection> Option(Action<FindOptions<T, TProjection>> option)
        {
            option(_options);
            return this;
        }

        /// <summary>
        /// Run the Find command in MongoDB server and get the results
        /// </summary>
        /// <returns>A list of entities</returns>
        public List<TProjection> Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Run the Find command in MongoDB server and get the results
        /// </summary>
        /// <returns>A list of entities</returns>
        async public Task<List<TProjection>> ExecuteAsync()
        {
            if (_sorts.Count > 0) _options.Sort = Builders<T>.Sort.Combine(_sorts);
            return await DB.FindAsync(_filter, _options);
        }
    }

    public enum Order
    {
        Ascending,
        Descending
    }
}
