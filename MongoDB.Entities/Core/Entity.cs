using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Core
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

        /// <summary>
        /// This property will be automatically set when an entity is updated.
        /// <para>TIP: This property is useful when sorting by update date.</para>
        /// </summary>
        public DateTime? ModifiedOn { get; set; }

        /// <summary>
        /// This property will be automatically set when an entity is created.
        /// <para>TIP: This property is useful when sorting by create date.</para>
        /// </summary>
        public DateTime? CreatedOn { get; set; }

    }
}
