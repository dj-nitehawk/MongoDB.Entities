using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Tests;

[Collection("PlaceInt64")]
public class PlaceInt64 : Place
{
    [BsonId]
    public Int64 Id { get; set; }

    public override object GenerateNewID()
          => Convert.ToInt64(DateTime.UtcNow.Ticks);

    public override bool HasDefaultID()
          => Id == 0;
}