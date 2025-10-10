using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;
using System;
using System.Threading;

namespace MongoDB.Entities.Tests;

[Collection("FlowerInt64")]
public class FlowerInt64 : Flower
{
    public static Int64 nextID = Int64.MaxValue;

    [BsonId]
    public long Id { get; set; }

    public override object GenerateNewID()
        => Interlocked.Decrement(ref nextID);

    public override bool HasDefaultID()
        => Id == 0;

    public FlowerInt64 NestedFlower { get; set; }
    public Many<CustomerWithCustomID, FlowerInt64> Customers { get; set; }

    public FlowerInt64()
    {
        this.InitOneToMany(() => Customers);
    }
}