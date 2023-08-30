using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[Collection("FlowerObjectId")]
public class FlowerObjectId : Flower
{
  [BsonId]
  public ObjectId? Id { get; set; }
  public override object GenerateNewID()
    => ObjectId.GenerateNewId();
  
  public FlowerObjectId NestedFlower { get; set; }
}