using System;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

public class ReviewGuid : Review
{
  [BsonId]
  public Guid? Id { get; set; }
  public override object GenerateNewID()
    => Guid.NewGuid();
  
  public Collection<BookGuid> Books { get; set; }
}