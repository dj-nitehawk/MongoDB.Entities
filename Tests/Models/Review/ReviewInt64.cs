using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.ObjectModel;

namespace MongoDB.Entities.Tests;

[Collection("ReviewInt64")]
public class ReviewInt64 : Review
{
    [BsonId]
    public Int64 Id { get; set; }
    public override object GenerateNewID()
      => Convert.ToInt64(DateTime.UtcNow.Ticks);

    public Collection<BookInt64> Books { get; set; }
}