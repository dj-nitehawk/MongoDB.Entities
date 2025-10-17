using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookObjectId")]
public class BookObjectId : Book
{
    [BsonId]
    public ObjectId ID { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId();

    public override bool HasDefaultID()
        => ObjectId.Empty == ID;

    public ReviewObjectId Review { get; set; }
    public ReviewObjectId[] ReviewArray { get; set; }
    public IList<ReviewObjectId> ReviewList { get; set; }
    public One<AuthorObjectId, ObjectId> MainAuthor { get; set; }

    public AuthorObjectId RelatedAuthor { get; set; }
    public AuthorObjectId[] OtherAuthors { get; set; }
    public Many<AuthorObjectId, BookObjectId> GoodAuthors { get; set; } = null!;
    public Many<AuthorObjectId, BookObjectId> BadAuthors { get; set; } = null!;

    [OwnerSide]
    public Many<GenreObjectId, BookObjectId> Genres { get; set; } = null!;

    public BookObjectId()
    {
        this.InitOneToMany(() => GoodAuthors);
        this.InitOneToMany(() => BadAuthors);
        this.InitManyToMany(() => Genres, g => g.Books);
    }
}