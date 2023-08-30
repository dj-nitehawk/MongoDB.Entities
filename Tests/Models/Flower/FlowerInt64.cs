using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerInt64")]
public class FlowerInt64 : Flower
{
  [BsonId]
  public Int64? Id { get; set; }
  public override object GenerateNewID()
    => Convert.ToInt64(DateTime.UtcNow.Ticks);
    
  public FlowerInt64 NestedFlower { get; set; }

}