using System;
using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorGuid")]
public class AuthorGuid : Author
{
  [BsonId]
  public string? ID { get; set; }
  public override object GenerateNewID()
      => Uuid7.NewUuid7().ToString();
  
  [BsonIgnoreIfDefault]
  public One<BookGuid> BestSeller { get; set; }

  public Many<BookGuid, AuthorGuid> Books { get; set; }

  [ObjectId]
  public string BookIDs { get; set; }

  public AuthorGuid() => this.InitOneToMany(() => Books!);
}
