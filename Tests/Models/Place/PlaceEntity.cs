using MongoDB.Bson;

namespace MongoDB.Entities.Tests;

[Collection("PlaceEntity")]
public class PlaceEntity : Place
{
    public string? Id { get; set; }
    public override object GenerateNewID()
        => ObjectId.GenerateNewId().ToString()!;
}
