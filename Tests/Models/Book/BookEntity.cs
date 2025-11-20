using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookEntity")]
public class BookEntity : Book
{
    [BsonId, AsObjectId]
    public string ID { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public override bool HasDefaultID()
        => string.IsNullOrEmpty(ID);

    public ReviewEntity Review { get; set; }
    public ReviewEntity[] ReviewArray { get; set; }
    public IList<ReviewEntity> ReviewList { get; set; }
    public One<AuthorEntity> MainAuthor { get; set; }

    public AuthorEntity RelatedAuthor { get; set; }
    public AuthorEntity[] OtherAuthors { get; set; }
    public Many<AuthorEntity, BookEntity> GoodAuthors { get; set; } = null!;
    public Many<AuthorEntity, BookEntity> BadAuthors { get; set; } = null!;

    [OwnerSide]
    public Many<GenreEntity, BookEntity> Genres { get; set; } = null!;

    public BookEntity()
    {
        this.InitOneToMany(() => GoodAuthors);
        this.InitOneToMany(() => BadAuthors);
        this.InitManyToMany(() => Genres, g => g.Books);
    }
}
