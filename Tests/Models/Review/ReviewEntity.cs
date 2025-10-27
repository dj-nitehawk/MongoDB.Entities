using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("ReviewEntity")]
public class ReviewEntity : Review
{
    [BsonId, AsObjectId]
    public string Id { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public Collection<BookEntity> Books { get; set; }
}