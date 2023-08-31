using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookInt64")]
public class BookInt64 : Book
{
  [BsonId]
  public Int64? ID { get; set; }
  public override object GenerateNewID()
    => Convert.ToInt64(DateTime.UtcNow.Ticks);
  
  public ReviewInt64 Review { get; set; }
  public ReviewInt64[] ReviewArray { get; set; }
  public IList<ReviewInt64> ReviewList { get; set; }
  public One<AuthorInt64> MainAuthor { get; set; }

  public AuthorInt64 RelatedAuthor { get; set; }
  public AuthorInt64[] OtherAuthors { get; set; }
  public Many<AuthorInt64, BookInt64> GoodAuthors { get; set; }
  public Many<AuthorInt64, BookInt64> BadAuthors { get; set; }

  [OwnerSide]
  public Many<GenreInt64, BookInt64> Genres { get; set; }

  public BookInt64()
  {
    this.InitOneToMany(() => GoodAuthors);
    this.InitOneToMany(() => BadAuthors);
    this.InitManyToMany(() => Genres, g => g.Books);
  }
  
}