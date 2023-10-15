using Medo;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorUuid")]
public class AuthorUuid : Author
{
    [BsonId]
    public string ID { get; set; }

    public override object GenerateNewID()
        => Uuid7.NewUuid7().ToString();

    public override bool HasDefaultID()
        => string.IsNullOrEmpty(ID);

    [BsonIgnoreIfDefault]
    public One<BookUuid> BestSeller { get; set; }

    public Many<BookUuid, AuthorUuid> Books { get; set; }

    [ObjectId]
    public string BookIDs { get; set; }

    public AuthorUuid()
    {
        this.InitOneToMany(() => Books);
    }
}