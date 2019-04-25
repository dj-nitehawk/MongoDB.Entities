using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities
{
    [BsonIgnoreExtraElements]
    public class Entity
    {
        /// <summary>
        /// This property is auto managed. Don't ever change this manually.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }

        public DateTime ModifiedOn { get; internal set; }
     }
}
