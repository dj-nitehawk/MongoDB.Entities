using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookObjectId")]
public class BookObjectId : Book
{
  [BsonId]
  public ObjectId? ID { get; set; }
  public override object GenerateNewID()
    => ObjectId.GenerateNewId();
  
  public ReviewObjectId Review { get; set; }
  public ReviewObjectId[] ReviewArray { get; set; }
  public IList<ReviewObjectId> ReviewList { get; set; }
  public One<AuthorObjectId> MainAuthor { get; set; }

  public AuthorObjectId RelatedAuthor { get; set; }
  public AuthorObjectId[] OtherAuthors { get; set; }
  public Many<AuthorObjectId, BookObjectId> GoodAuthors { get; set; }
  public Many<AuthorObjectId, BookObjectId> BadAuthors { get; set; }

  [OwnerSide]
  public Many<GenreObjectId, BookObjectId> Genres { get; set; }

  public BookObjectId()
  {
    this.InitOneToMany(() => GoodAuthors);
    this.InitOneToMany(() => BadAuthors);
    this.InitManyToMany(() => Genres, g => g.Books);
  }


}