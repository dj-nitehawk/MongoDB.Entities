using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("GenreObjectId")]
public class GenreObjectId : Genre
{
    [BsonId]
    public ObjectId ID { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId();

    [InverseSide]
    public Many<BookObjectId, GenreObjectId> Books { get; set; } = null!;

    public GenreObjectId()
    {
        this.InitManyToMany(() => Books, b => b.Genres);
    }
}