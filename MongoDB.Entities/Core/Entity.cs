﻿namespace MongoDB.Entities;

/// <summary>
/// Inherit this class for all entities you want to store in their own collection.
/// </summary>
public abstract class Entity : IEntity
{
    /// <summary>
    /// This property is auto managed. A new ID will be assigned for new entities upon saving.
    /// </summary>
    [BsonId, AsObjectId]
    public string? ID { get; set; }

    /// <summary>
    /// Override this method in order to control the generation of IDs for new entities.
    /// </summary>
    public virtual string GenerateNewID()
        => ObjectId.GenerateNewId().ToString();
}

/// <summary>
/// Inherit this class for all entities you want to store in their own collection.
/// </summary>
public abstract class Entity<TId> : IEntity<TId>
    where TId : IComparable<TId>, IEquatable<TId>
{
    /// <summary>
    /// This property is auto managed. A new ID will be assigned for new entities upon saving.
    /// </summary>
    [BsonId]
    public TId? ID { get; set; }

    /// <summary>
    /// Override this method in order to control the generation of IDs for new entities.
    /// </summary>
    public abstract TId GenerateNewID();
}