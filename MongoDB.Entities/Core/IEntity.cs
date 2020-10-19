namespace MongoDB.Entities
{
    /// <summary>
    /// The contract for Entity classes
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// The ID property for this entity type.
        /// <para>IMPORTANT: make sure to decorate this property with the [BsonId] attribute when implementing this interface</para>
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// Generate and set the value of the ID property from this method
        /// </summary>
        void SetNewID();
    }
}