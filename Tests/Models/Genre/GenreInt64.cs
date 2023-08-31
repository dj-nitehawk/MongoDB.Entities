using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("GenreInt64")]
public class GenreInt64 : Genre
{
  [BsonId]
  public Int64? ID { get; set; }
  public override object GenerateNewID()
    => Convert.ToInt64(DateTime.UtcNow.Ticks);

  [InverseSide]
  public Many<BookInt64, GenreInt64> Books { get; set; }

  public GenreInt64() => this.InitManyToMany(() => Books, b => b.Genres);

}