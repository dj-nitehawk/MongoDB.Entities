using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Entities
{
    internal static class Logic
    {
        internal static IEnumerable<UpdateDefinition<T>> BuildUpdateDefs<T>(T entity) where T : IEntity
        {
            if (entity == null)
                throw new ArgumentException("The supplied entity cannot be null!");

            var props = Cache<T>.UpdatableProps(entity);

            return props.Select(p => Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
        }

        internal static IEnumerable<UpdateDefinition<T>> BuildUpdateDefs<T>(T entity, Expression<Func<T, object>> members, bool excludeMode = false) where T : IEntity
        {
            var propNames = (members?.Body as NewExpression)?.Arguments
                .Select(a => a.ToString().Split('.')[1]);

            if (!propNames.Any())
                throw new ArgumentException("Unable to get any properties from the members expression!");

            var props = Cache<T>.UpdatableProps(entity);

            if (excludeMode)
                props = props.Where(p => !propNames.Contains(p.Name));
            else
                props = props.Where(p => propNames.Contains(p.Name));

            return props.Select(p => Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
        }

        internal static FilterDefinition<T> MergeWithGlobalFilter<T>(ConcurrentDictionary<Type, (object filterDef, bool prepend)> globalFilters, FilterDefinition<T> filter) where T : IEntity
        {
            if (globalFilters.Count > 0 && globalFilters.TryGetValue(typeof(T), out var gFilter))
            {
                var f = (FilterDefinition<T>)gFilter.filterDef;

                if (gFilter.prepend) return f & filter;

                return filter & f;
            }
            return filter;
        }
    }
}
