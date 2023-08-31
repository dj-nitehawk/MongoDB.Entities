using System;
using System.Collections.ObjectModel;
using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("ReviewUuid")]
public class ReviewUuid : Review
{
  [BsonId]
  public string? Id { get; set; }
  public override object GenerateNewID()
    => Uuid7.NewUuid7().ToString();
  
  public Collection<BookUuid> Books { get; set; }
}