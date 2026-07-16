using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests.Models;

// entities used to verify ID-generator resolution is lazy: cache/query/manual-ID paths
// work without a generator, while generation fails only when an empty ID needs one.

[Collection("NoGeneratorIntEntity")]
public class NoGeneratorIntEntity : IEntity
{
    [BsonId]
    public int ID { get; set; }

    public string Name { get; set; } = null!;
}

[Collection("EntityLevelOnlyIdEntity")]
public class EntityLevelOnlyIdEntity : IEntity
{
    [BsonId]
    public int ID { get; set; }

    public string Name { get; set; } = null!;
}
