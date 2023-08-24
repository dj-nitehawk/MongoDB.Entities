using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("PlaceGuid")]
public class PlaceGuid : Place
{
  [BsonId]
  public Guid? Id { get; set; }
  public override object GenerateNewID()
    => Guid.NewGuid();
}