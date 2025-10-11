using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Threading;

namespace MongoDB.Entities.Tests;

[Collection("PlaceInt64")]
public class PlaceInt64 : Place
{
    public static Int64 nextID = Int64.MaxValue;

    [BsonId]
    public Int64 Id { get; set; }

    public override object GenerateNewID()
        => Interlocked.Decrement(ref nextID);

    public override bool HasDefaultID()
          => Id == 0;
}