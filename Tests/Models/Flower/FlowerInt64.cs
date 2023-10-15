using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;
using System;

namespace MongoDB.Entities.Tests;

[Collection("FlowerInt64")]
public class FlowerInt64 : Flower
{
    [BsonId]
    public long Id { get; set; }

    public override object GenerateNewID()
        => Convert.ToInt64(DateTime.UtcNow.Ticks);

    public override bool HasDefaultID()
        => Id == 0;

    public FlowerInt64 NestedFlower { get; set; }
    public Many<CustomerWithCustomID, FlowerInt64> Customers { get; set; }

    public FlowerInt64()
    {
        this.InitOneToMany(() => Customers);
    }
}