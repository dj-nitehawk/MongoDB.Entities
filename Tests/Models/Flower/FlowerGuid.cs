using System;
using Medo;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerGuid")]
public class FlowerGuid : Flower
{
  [BsonId]
  public string? Id { get; set; }
  public override object GenerateNewID()
    => Uuid7.NewUuid7().ToString();
  
  public FlowerGuid NestedFlower { get; set; }
  public Many<CustomerWithCustomID, FlowerGuid> Customers { get; set; }
  public FlowerGuid()
  {
    this.InitOneToMany(() => Customers!);
  }
  
}
