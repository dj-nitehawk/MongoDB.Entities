using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("GenreEntity")]
public class GenreEntity : Genre
{
    [BsonId, AsObjectId]
    public string ID { get; set; }
    public override object GenerateNewID()
        => ObjectId.GenerateNewId().ToString()!;
    public override bool IsSetID()
        => !string.IsNullOrEmpty(ID);

    [InverseSide]
    public Many<BookEntity, GenreEntity> Books { get; set; } = null!;

    public GenreEntity() => this.InitManyToMany(() => Books, b => b.Genres);
}
