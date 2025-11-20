using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities;

/// <summary>
/// Use this attribute to mark a property in order to save it in MongoDB server as ObjectId
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ObjectIdAttribute() : BsonRepresentationAttribute(BsonType.ObjectId);

/// <summary>
/// Use this attribute to mark a string property to store the value in MongoDB as ObjectID if it is a valid ObjectId string.
/// If it is not a valid ObjectId string, it will be stored as string. This is useful when using custom formats for the ID field.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AsObjectIdAttribute() : BsonSerializerAttribute(typeof(ObjectIdSerializer))
{
    class ObjectIdSerializer : SerializerBase<string>, IRepresentationConfigurable
    {
        public BsonType Representation { get; set; }

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, string value)
        {
            if (string.IsNullOrEmpty(value))
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

        public override string Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        {
            switch (ctx.Reader.CurrentBsonType)
            {
                case BsonType.String:
                    return ctx.Reader.ReadString();

                case BsonType.ObjectId:
                    return ctx.Reader.ReadObjectId().ToString();

                case BsonType.Null:
                    ctx.Reader.ReadNull();

                    return null!;

                default:
                    throw new BsonSerializationException(
                        $"'{ctx.Reader.CurrentBsonType}' values are not valid on properties decorated with an [AsObjectId] attribute!");
            }
        }

        public IBsonSerializer WithRepresentation(BsonType representation)
            => throw new NotImplementedException();
    }
}

/// <summary>
/// Use this attribute to mark an object property to store the value in MongoDB as ObjectID if it is a valid ObjectId string.
/// If it is not a valid ObjectId string, it will be stored as string. This is needed for the join record so that the queryables
/// query based on the stored type of the field
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AsBsonIdAttribute() : BsonSerializerAttribute(typeof(ObjectIdSerializer))
{
    class ObjectIdSerializer : SerializerBase<object>, IRepresentationConfigurable
    {
        public BsonType Representation { get; set; }

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, object value)
        {
            switch (value)
            {
                case null:
                    ctx.Writer.WriteNull();

                    return;
                case ObjectId oId:
                    ctx.Writer.WriteObjectId(oId);

                    return;
                case string val:
                    if (val.Length == 24 && ObjectId.TryParse(val, out var oID))
                    {
                        ctx.Writer.WriteObjectId(oID);

                        return;
                    }
                    ctx.Writer.WriteString(val);

                    return;
                case long int64:
                    ctx.Writer.WriteInt64(int64);

                    return;
                default:
                    throw new BsonSerializationException($"'{value.GetType()}' values are not valid on properties decorated with an [AsBsonId] attribute!");
            }
        }

        public override object Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
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

                    return null!;

                case BsonType.Int64:
                    return ctx.Reader.ReadInt64();

                default:
                    throw new BsonSerializationException(
                        $"'{ctx.Reader.CurrentBsonType}' values are not valid on properties decorated with an [AsBsonId] attribute!");
            }
        }

        public IBsonSerializer WithRepresentation(BsonType representation)
            => throw new NotImplementedException();
    }
}