namespace MongoDB.Entities;

/// <summary>
/// The contract for Entity classes
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Generate and return a new ID string from this method. It will be used when saving new entities that don't have their ID set. 
    /// That is, if an entity has a null ID, this method will be called for getting a new ID value. 
    /// If you're not doing custom ID generation, simply do <c>return ObjectId.GenerateNewId().ToString()</c>
    /// </summary>
    object GenerateNewID();
    
    /// <summary>
    /// Used to check if the ID has been set to a valid value. The default value of the ID should return false.
    /// </summary>
    bool IsSetID();
    
}