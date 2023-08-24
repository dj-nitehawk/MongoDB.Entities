using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("PlaceObjectId")]
public class PlaceObjectId : Place
{
  [BsonId, AsObjectId]
  public ObjectId? Id { get; set; }
  public override object GenerateNewID()
    => ObjectId.GenerateNewId();
}