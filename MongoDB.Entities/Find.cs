using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public class Find<T> : Find<T, T> where T : Entity { }

    public class Find<T, TProjection> where T : Entity
    {
        private FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
        private List<SortDefinition<T>> _sorts = new List<SortDefinition<T>>();
        private FindOptions<T, TProjection> _options = new FindOptions<T, TProjection>();

        public TProjection By(string ID)
        {
            return ByAsync(ID).GetAwaiter().GetResult();

        }

        async public Task<TProjection> ByAsync(string ID)
        {
            Match(ID);
            return (await ExecuteAsync()).SingleOrDefault();
        }

        public List<TProjection> By(Expression<Func<T, bool>> expression)
        {
            return ByAsync(expression).GetAwaiter().GetResult();
        }

        async public Task<List<TProjection>> ByAsync(Expression<Func<T, bool>> expression)
        {
            Match(expression);
            return await ExecuteAsync();
        }

        public Find<T, TProjection> Match(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
        }

        public Find<T, TProjection> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        public Find<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            _filter = filter(Builders<T>.Filter);
            return this;
        }

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

        public Find<T, TProjection> Skip(int skipCount)
        {
            _options.Skip = skipCount;
            return this;
        }

        public Find<T, TProjection> Take(int takeCount)
        {
            _options.Limit = takeCount;
            return this;
        }

        public Find<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
        {
            _options.Projection = Builders<T>.Projection.Expression(expression);
            return this;
        }

        public Find<T, TProjection> Option(Action<FindOptions<T, TProjection>> option)
        {
            option(_options);
            return this;
        }

        public List<TProjection> Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

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

    //todo: xml docu

}
