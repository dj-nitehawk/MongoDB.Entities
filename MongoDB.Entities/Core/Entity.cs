using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities
{
    /// <summary>
    /// Inherit this class for all entities you want to store in their own collection.
    /// <para>TIP: If you want to store an entity in a particular database, use the [DatabaseAttribute]</para>
    /// </summary>
    [BsonIgnoreExtraElements]
    public abstract class Entity : IEntity
    {
        /// <summary>
        /// This property is auto managed. Don't ever change this manually.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }

    }
}
