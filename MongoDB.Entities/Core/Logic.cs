using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Entities
{
    internal static class Logic
    {
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
    }
}
