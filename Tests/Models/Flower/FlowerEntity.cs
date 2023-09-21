using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerEntity")]
public class FlowerEntity : Flower
{
    [BsonId, AsObjectId]
    public string Id { get; set; }
    public override object GenerateNewID()
        => ObjectId.GenerateNewId().ToString()!;
    public override bool IsSetID()
        => !string.IsNullOrEmpty(Id);

    public FlowerEntity NestedFlower { get; set; }
    public Many<CustomerWithCustomID, FlowerEntity> Customers { get; set; }
    public FlowerEntity()
    {
        this.InitOneToMany(() => Customers!);
    }
}