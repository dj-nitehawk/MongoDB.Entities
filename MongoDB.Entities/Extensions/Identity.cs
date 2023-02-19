using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Reflection;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the name of the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static string GetIdName<T>(this T _) where T : IEntity => Cache<T>.IdentityPropName;

    /// <summary>
    /// Gets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static object? GetId<T>(this T instance) where T : IEntity => Cache<T>.IdentityProp.GetValue(instance);

    /// <summary>
    /// Sets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id</typeparam>
    internal static void SetId<T>(this T instance, object? identity) where T : IEntity => Cache<T>.IdentityProp.SetValue(instance, identity);

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
}