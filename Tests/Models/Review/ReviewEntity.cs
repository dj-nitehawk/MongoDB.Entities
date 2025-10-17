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

    public override bool HasDefaultID()
      => string.IsNullOrEmpty(Id);

    public Collection<BookEntity> Books { get; set; }
}