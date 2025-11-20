namespace MongoDB.Entities;

/// <summary>
/// The contract for Entity classes
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Generate and return a new ID from this method. It will be used when saving new entities that don't have their ID set.
    /// I.e. if an entity has a default ID value (determined by calling `HasDefaultID()` /> method),
    /// this method will be called for obtaining a new ID value. If you're not doing custom ID generation, simply do
    /// <c>return ObjectId.GenerateNewId().ToString()</c>
    /// </summary>
    object GenerateNewID();
}