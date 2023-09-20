using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

public abstract class Review : IEntity
{
    [BsonRequired]
    public int Stars { get; set; }

    [BsonRequired]
    public string Reviewer { get; set; }

    public double Rating { get; set; }
    public FuzzyString Fuzzy { get; set; }
    public abstract object GenerateNewID();
}
