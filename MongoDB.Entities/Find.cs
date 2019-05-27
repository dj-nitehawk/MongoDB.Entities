using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public class Find<T> : Find<T, T> where T : Entity { }

    public class Find<T, TProjection> where T : Entity
    {
        private FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
        private List<SortDefinition<T>> _sorts = new List<SortDefinition<T>>();
        private FindOptions<T, TProjection> _options = new FindOptions<T, TProjection>();

        public Find<T, TProjection> Filter(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
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

    //todo: need these api methods

    //async public static Task<T> FindAsync<T>(string ID, FindOptions<T, T> options = null) where T : Entity
    //{
    //    return await (await GetCollection<T>().FindAsync(d => d.ID == ID, options)).SingleOrDefaultAsync();
    //}

    //async public static Task<List<T>> FindAsync<T>(Expression<Func<T, bool>> expression, FindOptions<T, T> options = null)
    //{

    //    return await (await GetCollection<T>().FindAsync(expression, options)).ToListAsync();
    //}

}
