namespace MongoDB.Entities.Configuration;

using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// References a document of a type, but doesn't store the physical reference
/// </summary>
/// <typeparam name="TOtherType"></typeparam>
public class MongoDBDocumentReference<TOtherType>
{
    //There shouldn't be any serializable members here
    public MongoDBDocumentReference()
    {
    }

    [BsonIgnore]
    public TOtherType? Cache { get; internal set; }
}
