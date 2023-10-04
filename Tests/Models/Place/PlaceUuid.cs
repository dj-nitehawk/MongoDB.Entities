using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("PlaceUuid")]
public class PlaceUuid : Place
{
    [BsonId]
    public string Id { get; set; }

    public override object GenerateNewID()
          => Uuid7.NewUuid7().ToString();

    public override bool HasDefaultID()
          => string.IsNullOrEmpty(Id);
}