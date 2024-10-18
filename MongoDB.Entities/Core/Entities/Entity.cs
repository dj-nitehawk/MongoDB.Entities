using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

/// <summary>
/// Inherit this class for all entities you want to store in their own collection.
/// </summary>
public abstract class Entity : IEntity
{
    /// <summary>
    /// This property is auto managed. A new ID will be assigned for new entities upon saving.
    /// </summary>
    [BsonId, AsObjectId]
    public string ID { get; set; } = null!;

    /// <summary>
    /// Override this method in order to control the generation of IDs for new entities.
    /// </summary>
    public virtual object GenerateNewID()
        => ObjectId.GenerateNewId().ToString()!;

    /// <inheritdoc />
    public virtual bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}