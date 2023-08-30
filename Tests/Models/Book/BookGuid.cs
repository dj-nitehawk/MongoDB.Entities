using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookGuid")]
public class BookGuid : Author
{
  [BsonId]
  public Guid? ID { get; set; }
  public override object GenerateNewID()
      => Guid.NewGuid();
}
