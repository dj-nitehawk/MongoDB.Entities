using Medo;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerUuid")]
public class FlowerUuid : Flower
{
    [BsonId] 
    public string Id { get; set; }
    public override object GenerateNewID()
      => Uuid7.NewUuid7().ToString();
    public override bool IsSetID()
      => !string.IsNullOrEmpty(Id);

    public FlowerUuid NestedFlower { get; set; }
    public Many<CustomerWithCustomID, FlowerUuid> Customers { get; set; }
    public FlowerUuid()
    {
        this.InitOneToMany(() => Customers!);
    }
}