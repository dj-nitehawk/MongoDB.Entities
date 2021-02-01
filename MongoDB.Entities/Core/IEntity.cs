namespace MongoDB.Entities
{
    /// <summary>
    /// The contract for Entity classes
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// The Id property for this entity type.
        /// <para>IMPORTANT: make sure to decorate this property with the [BsonId] attribute when implementing this interface</para>
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Generate and return a new Id string from this method. It will be used when saving new entities that don't have their Id set. 
        /// That is, if an entity has a null Id, this method will be called for getting a new Id value. 
        /// If you're not doing custom Id generation, simply do <c>return ObjectId.GenerateNewId().ToString()</c>
        /// </summary>
        string GenerateNewId();
    }
}