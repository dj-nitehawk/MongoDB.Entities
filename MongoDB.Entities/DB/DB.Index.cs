using System;

namespace MongoDB.Entities;

public static partial class DB
{
    /// <summary>
    /// Represents an index for a given IEntity
    /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static Index<T> Index<T>() where T : IEntity
    {
        return new Index<T>();
    }

    /// <summary>
    /// Represents an index for a given IEntity
    /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
    /// </summary>
    /// <param name="entity">The entity for which we will create the Index</param>
    public static Index<T> Index<T>(T entity) where T : IEntity
    {
        var genericType = typeof(Index<>).MakeGenericType(entity.GetType());
        var obj = Activator.CreateInstance(genericType);
        return (Index<T>) obj;
    }
}
