using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities;

[Collection("[SEQUENCE_COUNTERS]")]
internal class SequenceCounter : IEntity
{
    [BsonId] public string ID { get; set; } = null!;

    [BsonRepresentation(BsonType.Int64)]
    public ulong Count { get; set; }

    public object GenerateNewID()
        => throw new System.NotImplementedException();

    public bool IsSetID()
      => !string.IsNullOrEmpty(ID);
}