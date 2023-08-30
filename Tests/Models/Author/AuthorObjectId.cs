using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorObjectId")]
public class AuthorObjectId : Author
{
  [BsonId]
  public ObjectId? ID { get; set; }
  public override object GenerateNewID()
    => ObjectId.GenerateNewId();
}