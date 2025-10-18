using System;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the name of the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static string GetIdName<T>(this T _) where T : IEntity
        => Cache<T>.IdPropName;

    /// <summary>
    /// Gets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    internal static object GetId<T>(this T entity) where T : IEntity
        => Cache<T>.IdGetter(entity);

    /// <summary>
    /// Sets the Identity object
    /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id</typeparam>
    internal static void SetId<T>(this T entity, object id) where T : IEntity
        => Cache<T>.IdSetter(entity, id);
    
    // /// <summary>
    // /// When saving entities, this method will be called in order to determine if <see cref="GenerateNewID" /> needs to be called.
    // /// If this method returns <c>'true'</c>, <see cref="GenerateNewID" /> method is called and the ID (primary key) of the entity is populated.
    // /// If <c>'false'</c> is returned, it is assumed that ID generation is not required and the entity already has a non-default ID value.
    // /// </summary>
    /// <typeparam name="T">Any class that implements a MongoDB id</typeparam>
    internal static bool HasDefaultID<T>(this T entity) where T : IEntity
        => Equals(Cache<T>.IdGetter(entity), Cache<T>.IdDefaultValue);

    // /// <summary>
    // /// Generate and return a new ID from this method. It will be used when saving new entities that don't have their ID set.
    // /// I.e. if an entity has a default ID value (determined by calling <see cref="HasDefaultID" /> method),
    // /// this method will be called for obtaining a new ID value. If you're not doing custom ID generation, simply do
    // /// <c>return ObjectId.GenerateNewId().ToString()</c>
    // /// </summary>
    // internal static object GenerateNewID<T>(this T entity, DB? dbInstance=null) where T : IEntity
    // {
    //     return Cache<T>.BsonClassMap.IdMemberMap.IdGenerator.GenerateId(DB.InstanceOrDefault(dbInstance).Collection<T>(),entity);
    // }


}