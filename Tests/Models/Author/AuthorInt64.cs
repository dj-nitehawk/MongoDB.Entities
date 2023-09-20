using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Tests;

[Collection("AuthorInt64")]
public class AuthorInt64 : Author
{
    [BsonId]
    public Int64? ID { get; set; }
    public override object GenerateNewID()
      => Convert.ToInt64(DateTime.UtcNow.Ticks);

    [BsonIgnoreIfDefault]
    public One<BookInt64> BestSeller { get; set; }

    public Many<BookInt64, AuthorInt64> Books { get; set; }

    [ObjectId]
    public string BookIDs { get; set; }

    public AuthorInt64() => this.InitOneToMany(() => Books!);
}