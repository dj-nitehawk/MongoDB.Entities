using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookObjectId")]
public class BookObjectId : Book
{
  [BsonId]
  public ObjectId? ID { get; set; }
  public override object GenerateNewID()
    => ObjectId.GenerateNewId();
}