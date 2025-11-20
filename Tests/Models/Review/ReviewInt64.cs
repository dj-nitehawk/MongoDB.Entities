using System;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("ReviewInt64")]
public class ReviewInt64 : Review
{
    [BsonId]
    public long Id { get; set; }

    public override object GenerateNewID()
        => Convert.ToInt64(DateTime.UtcNow.Ticks);

    public override bool HasDefaultID()
        => Id == 0;

    public Collection<BookInt64> Books { get; set; }
}