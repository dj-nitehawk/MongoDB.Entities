using System;
using System.Threading;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("PlaceInt64")]
public class PlaceInt64 : Place
{
    public static Int64 nextID = Int64.MaxValue;

    [BsonId]
    public Int64 Id { get; set; }

    public override object GenerateNewID()
        => Interlocked.Decrement(ref nextID);
}