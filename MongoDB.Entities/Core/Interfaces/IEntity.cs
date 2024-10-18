namespace MongoDB.Entities;

/// <summary>
/// The contract for Entity classes
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Generate and return a new ID from this method. It will be used when saving new entities that don't have their ID set.
    /// I.e. if an entity has a default ID value (determined by calling <see cref="HasDefaultID" /> method),
    /// this method will be called for obtaining a new ID value. If you're not doing custom ID generation, simply do
    /// <c>return ObjectId.GenerateNewId().ToString()</c>
    /// </summary>
    object GenerateNewID();

    /// <summary>
    /// When saving entities, this method will be called in order to determine if <see cref="GenerateNewID" /> needs to be called.
    /// If this method returns <c>'true'</c>, <see cref="GenerateNewID" /> method is called and the ID (primary key) of the entity is populated.
    /// If <c>'false'</c> is returned, it is assumed that ID generation is not required and the entity already has a non-default ID value.
    /// </summary>
    bool HasDefaultID();
}