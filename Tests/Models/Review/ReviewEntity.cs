using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("ReviewEntity")]
public class ReviewEntity : Review
{
    [BsonId]
    public string Id { get; set; }

    public Collection<BookEntity> Books { get; set; }
}