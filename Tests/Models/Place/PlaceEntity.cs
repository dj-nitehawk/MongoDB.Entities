using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("PlaceEntity")]
public class PlaceEntity : Place
{
  [BsonId, AsObjectId]
  public string? Id { get; set; }
  public override object GenerateNewID()
      => ObjectId.GenerateNewId().ToString()!;
  public override bool IsSetID()
    => !string.IsNullOrEmpty(Id);
}
