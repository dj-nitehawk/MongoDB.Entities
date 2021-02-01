using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities
{
    /// <summary>
    /// Inherit this class for all entities you want to store in their own collection.
    /// </summary>
    public abstract class Entity : IEntity
    {
        /// <summary>
        /// This property is auto managed. A new Id will be assigned for new entities upon saving.
        /// </summary>
        [BsonId, ObjectId]
        public string Id { get; set; }

        /// <summary>
        /// Override this method in order to control the generation of Ids for new entities.
        /// </summary>
        public virtual string GenerateNewId()
            => ObjectId.GenerateNewId().ToString();
    }
}
