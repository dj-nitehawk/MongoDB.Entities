using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("ReviewObjectId")]
public class ReviewObjectId : Review
{
    [BsonId]
    public ObjectId Id { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId();

    public override bool HasDefaultID()
        => ObjectId.Empty == Id;

    public Collection<BookObjectId> Books { get; set; }
}