using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MongoDB.Entities
{
    public class Find<T> where T : Entity
    {
        private List<FilterDefinition<T>> _filters = new List<FilterDefinition<T>>();
        private SortDefinition<T> _sort;
        private ProjectionDefinition<T, T> _projection;
        private FindOptions<T, T> _options = new FindOptions<T>();

        public Find<T> Filter(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            _filters.Add(filter(Builders<T>.Filter));
            return this;
        }

        public Find<T> Sort(Expression<Func<T, object>> propertyToSortBy, Order sortOrder)
        {
            switch (sortOrder)
            {
                case Order.Ascending:
                    _sort = _sort.Ascending(propertyToSortBy);
                    break;
                case Order.Descending:
                    _sort = _sort.Descending(propertyToSortBy);
                    break;
            }

            return this;
        }

        public Find<T> Skip(int skipCount)
        {
            _options.Skip = skipCount;
            return this;
        }

        public Find<T> Take(int takeCount)
        {
            _options.Limit = takeCount;
            return this;
        }

        public Find<T> Project(Expression<Func<T, T>> projectionExpression)
        {
            _projection = Builders<T>.Projection.Expression(projectionExpression);
            return this;
        }
    }

    public enum Order
    {
        Ascending,
        Descending
    }

    //async public static Task<T> FindAsync<T>(string ID, FindOptions<T, T> options = null) where T : Entity
    //{
    //    return await (await GetCollection<T>().FindAsync(d => d.ID == ID, options)).SingleOrDefaultAsync();
    //}

    //async public static Task<List<T>> FindAsync<T>(Expression<Func<T, bool>> expression, FindOptions<T, T> options = null)
    //{

    //    return await (await GetCollection<T>().FindAsync(expression, options)).ToListAsync();
    //}

    //async public static Task<List<T>> FindAsync<T>(FilterDefinition<T> filter, FindOptions<T, T> options = null)
    //{
    //    return await (await GetCollection<T>().FindAsync(filter, options)).ToListAsync();
    //}
}
