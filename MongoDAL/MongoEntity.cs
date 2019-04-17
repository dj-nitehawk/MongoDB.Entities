using MongoDB.Bson;
using Newtonsoft.Json;

namespace MongoDAL
{
    public class MongoEntity
    {
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId Id { get; set; }
    }
}
