using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("AuthorObjectId")]
public class AuthorObjectId : Author
{
    [BsonId]
    public ObjectId ID { get; set; }

    public override object GenerateNewID()
        => ObjectId.GenerateNewId();

    [BsonIgnoreIfDefault]
    public One<BookObjectId, ObjectId> BestSeller { get; set; }

    public Many<BookObjectId, AuthorObjectId> Books { get; set; }

    [ObjectId]
    public string BookIDs { get; set; }

    public AuthorObjectId()
    {
        this.InitOneToMany(() => Books);
    }
}