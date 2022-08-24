using MongoDB.Bson.Serialization.Attributes;
using System.Collections.ObjectModel;

namespace MongoDB.Entities.Tests;

public class Review : Entity
{
    [BsonRequired]
    public int Stars { get; set; }

    [BsonRequired]
    public string Reviewer { get; set; }

    public double Rating { get; set; }
    public FuzzyString Fuzzy { get; set; }
    public Collection<Book> Books { get; set; }
}
