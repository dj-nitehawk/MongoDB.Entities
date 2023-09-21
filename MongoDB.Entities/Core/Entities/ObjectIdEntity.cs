﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

/// <summary>
/// Inherit this class for all entities you want to store in their own collection.
/// </summary>
public abstract class ObjectIdEntity : IEntity
{
    /// <summary>
    /// This property is auto managed. A new ID will be assigned for new entities upon saving.
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Override this method in order to control the generation of IDs for new entities.
    /// </summary>
    public virtual object GenerateNewID()
      => ObjectId.GenerateNewId();

    /// <summary>
    /// Used to check if the ID has been set to a valid value. The default value of the ID should return false.
    /// </summary>
    /// <returns>true if the ID has been set</returns>
    public bool IsSetID()
      => ObjectId.Empty != Id;

}