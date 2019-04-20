using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace MongoDAL
{
    [BsonIgnoreExtraElements]
    public class MongoEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [JsonIgnore]
        public DateTime ModifiedOn { get; set; }
    }
}
