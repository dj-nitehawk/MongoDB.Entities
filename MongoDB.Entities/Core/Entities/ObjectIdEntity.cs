using MongoDB.Bson;
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

    public object GenerateNewID()
        => ObjectId.GenerateNewId();
}