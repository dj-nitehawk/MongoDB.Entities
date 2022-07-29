namespace MongoDB.Entities.Configuration;

using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// References a collection of a type
/// </summary>
/// <typeparam name="TOtherType"></typeparam>
public class MongoDBCollectionReference<TOtherType>
{
    //There shouldn't be any serializable members here
    public MongoDBCollectionReference()
    {
    }


    [BsonIgnore]
    public List<TOtherType>? Cache { get; internal set; }
}

