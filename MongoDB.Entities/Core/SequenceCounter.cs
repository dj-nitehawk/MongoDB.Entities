using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Core;

namespace MongoDB.Entities
{
    [Name("[SEQUENCE_COUNTERS]")]
    internal class SequenceCounter : IEntity
    {
        [BsonId]
        public string ID { get; set; }

        [BsonRepresentation(BsonType.Int64)]
        public ulong Count { get; set; }
    }
}
