using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Entities;

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
/// Indicates that this property is the owner side of a many-to-many relationship
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OwnerSideAttribute : Attribute { }

/// <summary>
/// Indicates that this property is the inverse side of a many-to-many relationship
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class InverseSideAttribute : Attribute { }

/// <summary>
/// Specifies a custom MongoDB collection name for an entity type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CollectionAttribute : Attribute
{
    public string Name { get; }

    public CollectionAttribute(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        Name = name;
    }
}

/// <summary>
/// Use this attribute on properties that you want to omit when using SavePreserving() instead of supplying an expression. 
/// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving() 
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PreserveAttribute : Attribute { }

/// <summary>
/// Properties that don't have this attribute will be omitted when using SavePreserving()
/// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving()
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DontPreserveAttribute : Attribute { }

/// <summary>
/// Use this attribute to mark a property in order to save it in MongoDB server as ObjectId
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ObjectIdAttribute : BsonRepresentationAttribute
{
    public ObjectIdAttribute() : base(BsonType.ObjectId)
    { }
}

/// <summary>
/// Use this attribute to mark a string property to store the value in MongoDB as ObjectID if it is a valid ObjectId string. 
/// If it is not a valid ObjectId string, it will be stored as string. This is useful when using custom formats for the ID field.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AsObjectIdAttribute : BsonSerializerAttribute
{
    public AsObjectIdAttribute() : base(typeof(ObjectIdSerializer)) { }

    private class ObjectIdSerializer : SerializerBase<string?>, IRepresentationConfigurable
    {
        public BsonType Representation { get; set; }

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, string? value)
        {
            if (value == null)
            {
                ctx.Writer.WriteNull();
                return;
            }

            if (value.Length == 24 && ObjectId.TryParse(value, out var oID))
            {
                ctx.Writer.WriteObjectId(oID);
                return;
            }

            ctx.Writer.WriteString(value);
        }

        public override string? Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        {
            switch (ctx.Reader.CurrentBsonType)
            {
                case BsonType.String:
                    return ctx.Reader.ReadString();

                case BsonType.ObjectId:
                    return ctx.Reader.ReadObjectId().ToString();

                case BsonType.Null:
                    ctx.Reader.ReadNull();
                    return null;

                default:
                    throw new BsonSerializationException($"'{ctx.Reader.CurrentBsonType}' values are not valid on properties decorated with an [AsObjectId] attribute!");
            }
        }

        public IBsonSerializer WithRepresentation(BsonType representation) => throw new NotImplementedException();
    }
}

/// <summary>
/// Use this attribute to mark an object property to store the value in MongoDB as ObjectID if it is a valid ObjectId string. 
/// If it is not a valid ObjectId string, it will be stored as string. This is needed for the join record so that the queryables
/// query based on the stored type of the field
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AsBsonIdAttribute : BsonSerializerAttribute
{
    public AsBsonIdAttribute() : base(typeof(ObjectIdSerializer)) { }

    private class ObjectIdSerializer : SerializerBase<object?>, IRepresentationConfigurable
    {
        public BsonType Representation { get; set; }

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, object? value)
        {
            if (value == null)
            {
                ctx.Writer.WriteNull();
                return;
            }

            if (value is ObjectId oId)
            {
                ctx.Writer.WriteObjectId(oId);
                return;
            }

            if (value is string vStr)
            {
                if (vStr.Length == 24 && ObjectId.TryParse(vStr, out var oID))
                {
                    ctx.Writer.WriteObjectId(oID);
                    return;
                }

                ctx.Writer.WriteString(vStr);
                return;
            }

            throw new BsonSerializationException($"'{value.GetType()}' values are not valid on properties decorated with an [AsBsonId] attribute!");
        }

        public override object? Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        {
            switch (ctx.Reader.CurrentBsonType)
            {
                case BsonType.String:
                    return ctx.Reader.ReadString();

                case BsonType.ObjectId:
                    if (args.NominalType == typeof(ObjectId))
                        return ctx.Reader.ReadObjectId();
                    return ctx.Reader.ReadObjectId().ToString();

                case BsonType.Null:
                    ctx.Reader.ReadNull();
                    return null;

                default:
                    throw new BsonSerializationException($"'{ctx.Reader.CurrentBsonType}' values are not valid on properties decorated with an [AsBsonId] attribute!");
            }
        }

        public IBsonSerializer WithRepresentation(BsonType representation) => throw new NotImplementedException();
    }
}
