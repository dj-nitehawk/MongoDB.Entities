namespace MongoDB.Entities;

internal static class Logic
{
    internal static IEnumerable<UpdateDefinition<T>> BuildUpdateDefs<T>(T entity, DBContext context)
    {
        if (entity == null)
            throw new ArgumentException("The supplied entity cannot be null!");

        var props = context.Cache<T>().UpdatableProps(entity);

        return props.Select(p => Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
    }

    internal static IEnumerable<UpdateDefinition<T>> BuildUpdateDefs<T>(T entity, Expression<Func<T, object>> members, DBContext context, bool excludeMode = false)
    {
        var propNames = (members?.Body as NewExpression)?.Arguments
            .Select(a => a.ToString().Split('.')[1]);

        if (!propNames.Any())
            throw new ArgumentException("Unable to get any properties from the members expression!");

        var props = context.Cache<T>().UpdatableProps(entity);

        if (excludeMode)
            props = props.Where(p => !propNames.Contains(p.Name));
        else
            props = props.Where(p => propNames.Contains(p.Name));

        return props.Select(p => Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
    }

    internal static FilterDefinition<T> MergeWithGlobalFilter<T>(bool ignoreGlobalFilters, Dictionary<Type, (object filterDef, bool prepend)>? globalFilters, FilterDefinition<T> filter)
    {
        //WARNING: this has to do the same thing as DBContext.Pipeline.MergeWithGlobalFilter method
        //         if the following logic changes, update the other method also

        if (!ignoreGlobalFilters && globalFilters is not null && globalFilters.Count > 0 && globalFilters.TryGetValue(typeof(T), out var gFilter))
        {
            switch (gFilter.filterDef)
            {
                case FilterDefinition<T> definition:
                    return (gFilter.prepend) ? definition & filter : filter & definition;

                case BsonDocument bsonDoc:
                    return (gFilter.prepend) ? bsonDoc & filter : filter & bsonDoc;

                case string jsonString:
                    return (gFilter.prepend) ? jsonString & filter : filter & jsonString;
            }
        }
        return filter;
    }
}
