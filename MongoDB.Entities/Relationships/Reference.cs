using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities
{
    internal class Reference : Entity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ParentID { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ChildID { get; set; }
    }
}
