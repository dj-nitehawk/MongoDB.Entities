using System.Collections.Generic;
using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("BookUuid")]
public class BookUuid : Book
{
    [BsonId]
    public string ID { get; set; }

    public override object GenerateNewID()
        => Uuid7.NewUuid7().ToString();

    public override bool HasDefaultID()
        => string.IsNullOrEmpty(ID);

    public ReviewUuid Review { get; set; }
    public ReviewUuid[] ReviewArray { get; set; }
    public IList<ReviewUuid> ReviewList { get; set; }
    public One<AuthorUuid> MainAuthor { get; set; }
    public AuthorUuid RelatedAuthor { get; set; }
    public AuthorUuid[] OtherAuthors { get; set; }
    public Many<AuthorUuid, BookUuid> GoodAuthors { get; set; } = null!;
    public Many<AuthorUuid, BookUuid> BadAuthors { get; set; } = null!;

    [OwnerSide]
    public Many<GenreUuid, BookUuid> Genres { get; set; } = null!;

    public BookUuid()
    {
        this.InitOneToMany(() => GoodAuthors);
        this.InitOneToMany(() => BadAuthors);
        this.InitManyToMany(() => Genres, g => g.Books);
    }
}