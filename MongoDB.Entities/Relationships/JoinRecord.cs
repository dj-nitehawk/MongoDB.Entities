using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Core;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a parent-child relationship between two entities.
    /// <para>TIP: The ParentID and ChildID switches around for many-to-many relationships depending on the side of the relationship you're accessing.</para>
    /// </summary>
    public class JoinRecord : Entity
    {
        /// <summary>
        /// The ID of the parent IEntity for both one-to-many and the owner side of many-to-many relationships.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string ParentID { get; set; }

        /// <summary>
        /// The ID of the child IEntity in one-to-many relationships and the ID of the inverse side IEntity in many-to-many relationships.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string ChildID { get; set; }
    }
}
