using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities;

/// <summary>
/// Specifies the field name and/or the order of the persisted document.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FieldAttribute : BsonElementAttribute
{
    public FieldAttribute(int fieldOrder) { Order = fieldOrder; }
    public FieldAttribute(string fieldName) : base(fieldName) { }
    public FieldAttribute(string fieldName, int fieldOrder) : base(fieldName) { Order = fieldOrder; }
}

/// <summary>
/// Specifies a custom MongoDB collection name for an entity type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CollectionAttribute : System.Attribute
{
    public string Name { get; }

    public CollectionAttribute(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        Name = name;
    }
}

/// <summary>
/// Use this attribute to ignore a property when persisting an entity to the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IgnoreAttribute : BsonIgnoreAttribute { }

/// <summary>
/// Use this attribute to ignore a property when persisting an entity to the database if the value is null/default.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IgnoreDefaultAttribute : BsonIgnoreIfDefaultAttribute { }
