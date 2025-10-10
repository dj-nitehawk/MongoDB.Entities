﻿using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Threading;

namespace MongoDB.Entities.Tests;

[Collection("GenreInt64")]
public class GenreInt64 : Genre
{
    public static Int64 nextID = Int64.MaxValue;

    [BsonId]
    public Int64 ID { get; set; }

    public override object GenerateNewID()
        => Interlocked.Decrement(ref nextID);

    public override bool HasDefaultID()
        => ID == 0;

    [InverseSide]
    public Many<BookInt64, GenreInt64> Books { get; set; } = null!;

    public GenreInt64()
    {
        this.InitManyToMany(() => Books, b => b.Genres);
    }
}