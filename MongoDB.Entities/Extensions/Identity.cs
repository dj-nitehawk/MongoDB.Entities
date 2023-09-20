using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the name of the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static string GetIdName<T>(this T _) where T : IEntity => Cache<T>.IdPropName;

    /// <summary>
    /// Gets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static object GetId<T>(this T entity) where T : IEntity => Cache<T>.IdGetter(entity);

    /// <summary>
    /// Sets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id</typeparam>
    internal static void SetId<T>(this T entity, object id) where T : IEntity => Cache<T>.IdSetter(entity, id);

    /// <summary>
    /// Gets the PropertyInfo for the Identity object
    /// </summary>
    /// <param name="type">Any class that implements a MongoDB id</param>
    internal static PropertyInfo? GetIdPropertyInfo(this Type type)
    {
        // Let's get the identity Property based on the MongoDB identity rules
        return Array.Find(type.GetProperties(), p =>
            p.Name.Equals("_id", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
            p.IsDefined(typeof(BsonIdAttribute), true));
    }

    internal static Func<object, object> GetterForProp(this Type source, string propertyName)
    {
        //(object parent, object returnVal) => ((object)((TParent)parent).property);

        var parent = Expression.Parameter(typeof(object));
        var property = Expression.Property(Expression.Convert(parent, source), propertyName);
        var convertProp = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<object, object>>(convertProp, parent).Compile();
    }

    internal static Action<object, object> SetterForProp(this Type source, string propertyName)
    {
        //(object parent, object value) => ((TParent)parent).property = (TProp)value;

        var parent = Expression.Parameter(typeof(object));
        var value = Expression.Parameter(typeof(object));
        var property = Expression.Property(Expression.Convert(parent, source), propertyName);
        var body = Expression.Assign(property, Expression.Convert(value, property.Type));

        return Expression.Lambda<Action<object, object>>(body, parent, value).Compile();
    }
}