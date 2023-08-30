using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerGuid")]
public class FlowerGuid : Flower
{
  [BsonId]
  public Guid? Id { get; set; }
  public override object GenerateNewID()
      => Guid.NewGuid();
  
  public FlowerGuid NestedFlower { get; set; }
}
