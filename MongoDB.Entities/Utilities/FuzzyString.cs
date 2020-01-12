using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;
using System;

namespace MongoDB.Entities
{
    internal class FuzzyStringSerializer : SerializerBase<FuzzyString>, IBsonDocumentSerializer
    {
        private static readonly BsonDocumentSerializer docSerializer = new BsonDocumentSerializer();
        private static readonly StringSerializer strSerializer = new StringSerializer();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FuzzyString fString)
        {
            if (fString.Value.Length > 250) throw new NotSupportedException("FuzzyString can only hold a maximum of 250 characters!");

            context.Writer.WriteStartDocument();
            context.Writer.WriteString("Value", fString.Value);
            context.Writer.WriteString("Hash",
                string.Join(" ",
                    DoubleMetaphone.GetKeys(fString.Value)));
            context.Writer.WriteEndDocument();
        }

        public override FuzzyString Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return new FuzzyString
            {
                Value = docSerializer.Deserialize(context, args)
                                     .GetValue("Value").AsString
            };
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

    public class FuzzyString
    {
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
