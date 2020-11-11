using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Entities
{
    internal class FuzzyStringSerializer : SerializerBase<FuzzyString>, IBsonDocumentSerializer
    {
        private static readonly StringSerializer strSerializer = new StringSerializer();

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, FuzzyString fString)
        {
            if (fString == null || string.IsNullOrWhiteSpace(fString.Value))
            {
                ctx.Writer.WriteNull();
            }
            else
            {
                if (fString.Value.Length > FuzzyString.CharacterLimit)
                    throw new NotSupportedException($"FuzzyString can only hold a maximum of {FuzzyString.CharacterLimit} characters!");

                ctx.Writer.WriteStartDocument();
                ctx.Writer.WriteString("Value", fString.Value);
                ctx.Writer.WriteString("Hash", fString.Value.ToDoubleMetaphoneHash());
                ctx.Writer.WriteEndDocument();
            }
        }

        public override FuzzyString Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        {
            var bsonType = ctx.Reader.GetCurrentBsonType();

            switch (bsonType)
            {
                case BsonType.Document:

                    string value = null;

                    ctx.Reader.ReadStartDocument();
                    while (ctx.Reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        if (ctx.Reader.ReadName() == "Value")
                            value = ctx.Reader.ReadString();
                        else
                            ctx.Reader.SkipValue();
                    }
                    ctx.Reader.ReadEndDocument();

                    if (value == null)
                        throw new FormatException("Unable to deserialize a value from the FuzzyString document!");

                    return value;

                case BsonType.Null:
                    ctx.Reader.ReadNull();
                    return null;

                default:
                    throw new FormatException($"Cannot deserialize a FuzzyString value from a [{bsonType}]");
            }
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            switch (memberName)
            {
                case "Value":
                    serializationInfo = new BsonSerializationInfo("Value", strSerializer, typeof(string));
                    return true;
                default:
                    serializationInfo = null;
                    return false;
            }
        }
    }

    /// <summary>
    /// Use this type to store strings if you need fuzzy text searching with MongoDB
    /// <para>TIP: There's a default limit of 250 characters for ensuring best performance. 
    /// If you exceed the default limit, an exception will be thrown. 
    /// You can increase the limit by sacrificing performance/resource utilization by setting the static property 
    /// <c>FuzzyString.CharacterLimit = 500</c> at startup.</para>
    /// </summary>
    public class FuzzyString
    {
        public static int CharacterLimit { get; set; } = 250;

        public string Value { get; set; }

        public static implicit operator FuzzyString(string value)
        {
            return new FuzzyString { Value = value };
        }

        public static implicit operator string(FuzzyString fuzzyString)
        {
            return fuzzyString.Value;
        }
    }
}
