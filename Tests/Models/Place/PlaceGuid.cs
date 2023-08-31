using System;
using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("PlaceGuid")]
public class PlaceGuid : Place
{
  [BsonId]
  public string? Id { get; set; }
  public override object GenerateNewID()
    => Uuid7.NewUuid7().ToString();
}