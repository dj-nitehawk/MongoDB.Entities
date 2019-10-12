using MongoDB.Driver;
using MongoDB.Entities.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a MongoDB Find command
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Find<T> : Find<T, T> where T : IEntity
    {
        internal Find(IClientSessionHandle session = null, string db = null) : base(session, db) { }
    }

    /// <summary>
    /// Represents a MongoDB Find command
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
    public class Find<T, TProjection> where T : IEntity
    {
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private readonly Collection<SortDefinition<T>> sorts = new Collection<SortDefinition<T>>();
        private readonly FindOptions<T, TProjection> options = new FindOptions<T, TProjection>();
        private readonly IClientSessionHandle session = null;
        private string db = null;

        internal Find(IClientSessionHandle session = null, string db = null)
        {
            this.session = session;
            this.db = db;
        }

        /// <summary>
        /// Find a single IEntity by ID
        /// </summary>
        /// <param name="ID">The unique ID of an IEntity</param>
        /// <returns>A single entity or null if not found</returns>
        public TProjection One(string ID)
        {
            return OneAsync(ID).GetAwaiter().GetResult();

        }

        /// <summary>
        /// Find a single IEntity by ID
        /// </summary>
        /// <param name="ID">The unique ID of an IEntity</param>
        /// <returns>A single entity or null if not found</returns>
        public async Task<TProjection> OneAsync(string ID)
        {
            Match(ID);
            return (await ExecuteAsync()).SingleOrDefault();
        }

        /// <summary>
        /// Find entities by supplying a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        /// <returns>A list of Entities</returns>
        public List<TProjection> Many(Expression<Func<T, bool>> expression)
        {
            return ManyAsync(expression).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Find entities by supplying a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        /// <returns>A list of Entities</returns>
        public async Task<List<TProjection>> ManyAsync(Expression<Func<T, bool>> expression)
        {
            Match(expression);
            return await ExecuteAsync();
        }

        /// <summary>
        /// Find entities by supplying a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <returns>A list of Entities</returns>
        public List<TProjection> Many(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            return ManyAsync(filter).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Find entities by supplying a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <returns>A list of Entities</returns>
        public async Task<List<TProjection>> ManyAsync(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            Match(filter);
            return await ExecuteAsync();
        }

        /// <summary>
        /// Specify an IEntity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique IEntity ID</param>
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
        /// Specify the matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Find<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter = filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        public Find<T, TProjection> MatchExpression(string expression)
        {
            this.filter = "{$expr:" + expression + "}";
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
                    sorts.Add(Builders<T>.Sort.Ascending(propertyToSortBy));
                    break;
                case Order.Descending:
                    sorts.Add(Builders<T>.Sort.Descending(propertyToSortBy));
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
            options.Skip = skipCount;
            return this;
        }

        /// <summary>
        /// Specify how many entiteis to Take/Limit
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
            options.Projection = Builders<T>.Projection.Expression(expression);
            return this;
        }

        /// <summary>
        /// Specify how to project the results using a projection expression
        /// </summary>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <returns></returns>
        public Find<T, TProjection> Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
        {
            options.Projection = projection(Builders<T>.Projection);
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
        public async Task<List<TProjection>> ExecuteAsync()
        {
            if (sorts.Count > 0) options.Sort = Builders<T>.Sort.Combine(sorts);
            return await DB.FindAsync(filter, options, session, db);
        }

    }

    public enum Order
    {
        Ascending,
        Descending
    }
}
