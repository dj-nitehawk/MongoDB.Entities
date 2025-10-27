using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorEntity")]
public class AuthorEntity : Author
{
    [BsonId, AsObjectId]
    public string ID { get; set; } = null!;

    public override object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    [BsonIgnoreIfDefault]
    public One<BookEntity> BestSeller { get; set; }

    public Many<BookEntity, AuthorEntity> Books { get; set; }

    [ObjectId]
    public string BookIDs { get; set; }

    public AuthorEntity()
    {
        this.InitOneToMany(() => Books);
    }
}