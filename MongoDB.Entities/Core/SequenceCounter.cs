using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities
{
    [Collection("[SEQUENCE_COUNTERS]")]
    internal class SequenceCounter : IEntity
    {
        [BsonId]
        public string ID { get; set; }

        [BsonRepresentation(BsonType.Int64)]
        public ulong Count { get; set; }

        public string GenerateNewID()
            => throw new System.NotImplementedException();
    }
}
