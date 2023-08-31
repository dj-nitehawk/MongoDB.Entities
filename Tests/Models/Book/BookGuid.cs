using System;
using System.Collections.Generic;
using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookGuid")]
public class BookGuid : Book
{
  [BsonId]
  public string? ID { get; set; }
  public override object GenerateNewID()
    => Uuid7.NewUuid7().ToString();

  public ReviewGuid Review { get; set; }
  public ReviewGuid[] ReviewArray { get; set; }
  public IList<ReviewGuid> ReviewList { get; set; }
  public One<AuthorGuid> MainAuthor { get; set; }

  public AuthorGuid RelatedAuthor { get; set; }
  public AuthorGuid[] OtherAuthors { get; set; }
  public Many<AuthorGuid, BookGuid> GoodAuthors { get; set; }
  public Many<AuthorGuid, BookGuid> BadAuthors { get; set; }

  [OwnerSide]
  public Many<GenreGuid, BookGuid> Genres { get; set; }

  public BookGuid()
  {
    this.InitOneToMany(() => GoodAuthors);
    this.InitOneToMany(() => BadAuthors);
    this.InitManyToMany(() => Genres, g => g.Books);
  }

}
