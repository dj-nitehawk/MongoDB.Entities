namespace MongoDB.Entities;

/// <summary>
/// The contract for Entity classes
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Generate and return a new ID from this method. It will be used when saving new entities that don't have their ID set.
    /// I.e. if an entity's ID property holds the default value of its type (null for strings, ObjectId.Empty, 0, Guid.Empty, etc.),
    /// this method will be called for obtaining a new ID value. If you're not doing custom ID generation, simply do
    /// <c>return ObjectId.GenerateNewId().ToString()</c>
    /// </summary>
    object GenerateNewID();
}