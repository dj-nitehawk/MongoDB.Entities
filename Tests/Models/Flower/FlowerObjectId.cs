using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerObjectId")]
public class FlowerObjectId : Flower
{
    [BsonId]
    public ObjectId Id { get; set; }
    public FlowerObjectId NestedFlower { get; set; }
    public Many<CustomerWithCustomID, FlowerObjectId> Customers { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId();

    public override bool HasDefaultID()
        => ObjectId.Empty == Id;

    public FlowerObjectId()
    {
        this.InitOneToMany(() => Customers!);
    }
}