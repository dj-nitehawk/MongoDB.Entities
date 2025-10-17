using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests;

[Collection("Writer")]
public abstract class Author : IEntity, IModifiedOn, ICreatedOn
{
    public string Name { get; set; }
    public string Surname { get; set; }

    [BsonIgnoreIfNull]
    public string? FullName { get; set; }

    [Preserve]
    public Date Birthday { get; set; }

    [Preserve]
    public int Age { get; set; }

    [BsonIgnoreIfDefault, Preserve]
    public int Age2 { get; set; }

    public DateTime ModifiedOn { get; set; }

    public ModifiedBy UpdatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public abstract object GenerateNewID();
    public abstract bool HasDefaultID();
}