using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;
using System;

namespace MongoDB.Entities.Tests;

[Collection("FlowerInt64")]
public class FlowerInt64 : Flower
{
    [BsonId]
    public Int64 Id { get; set; }
    public override object GenerateNewID()
      => Convert.ToInt64(DateTime.UtcNow.Ticks);
    public override bool IsSetID()
      => Id!=0;

    public FlowerInt64 NestedFlower { get; set; }
    public Many<CustomerWithCustomID, FlowerInt64> Customers { get; set; }
    public FlowerInt64()
    {
        this.InitOneToMany(() => Customers!);
    }
}