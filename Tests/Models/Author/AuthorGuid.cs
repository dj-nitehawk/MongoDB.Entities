using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorGuid")]
public class AuthorGuid : Author
{
  [BsonId]
  public Guid? ID { get; set; }
  public override object GenerateNewID()
      => Guid.NewGuid();
}
