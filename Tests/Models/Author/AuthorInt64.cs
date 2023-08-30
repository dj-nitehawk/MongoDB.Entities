using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorInt64")]
public class AuthorInt64 : Author
{
  [BsonId]
  public Int64? ID { get; set; }
  public override object GenerateNewID()
    => Convert.ToInt64(DateTime.UtcNow.Ticks);}