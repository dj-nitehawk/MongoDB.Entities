using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Core;
using System;

namespace MongoDB.Entities
{
    [Name("[SEQUENCE_COUNTERS]")]
    internal class SequenceCounter : IEntity
    {
        [BsonId]
        public string ID { get; set; }

        [BsonRepresentation(BsonType.Int64)]
        public ulong Count { get; set; }

        [Ignore]
        public DateTime ModifiedOn
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
