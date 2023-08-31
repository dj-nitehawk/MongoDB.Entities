using System;
using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("GenreGuid")]
public class GenreGuid : Genre
{
  [BsonId]
  public string? ID { get; set; }
  public override object GenerateNewID()
    => Uuid7.NewUuid7().ToString();

  [InverseSide]
  public Many<BookGuid, GenreGuid> Books { get; set; }

  public GenreGuid() => this.InitManyToMany(() => Books, b => b.Genres);

}