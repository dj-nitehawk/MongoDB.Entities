﻿using System;

namespace MongoDB.Entities.Tests;

[Collection("Writer")]
public abstract class Author : IEntity, IModifiedOn, ICreatedOn
{
    public string Name { get; set; }
    public string Surname { get; set; }

    [Bson.Serialization.Attributes.BsonIgnoreIfNull]
    public string? FullName { get; set; }

    [Preserve]
    public Date Birthday { get; set; }

    [Preserve]
    public int Age { get; set; }

    [Bson.Serialization.Attributes.BsonIgnoreIfDefault]
    [Preserve]
    public int Age2 { get; set; }

    public DateTime ModifiedOn { get; set; }

    public ModifiedBy UpdatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public abstract object GenerateNewID();
}
