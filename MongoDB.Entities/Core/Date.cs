using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Entities
{
    internal class DateSerializer : SerializerBase<Date>, IBsonDocumentSerializer
    {
        private static readonly Int64Serializer longSerializer = new Int64Serializer();
        private static readonly DateTimeSerializer dtSerializer = new DateTimeSerializer();

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, Date date)
        {
            if (date == null)
            {
                ctx.Writer.WriteNull();
            }
            else
            {
                var dtUTC = BsonUtils.ToUniversalTime(date.DateTime);
                ctx.Writer.WriteStartDocument();
                ctx.Writer.WriteDateTime("DateTime", BsonUtils.ToMillisecondsSinceEpoch(dtUTC));
                ctx.Writer.WriteInt64("Ticks", dtUTC.Ticks);
                ctx.Writer.WriteEndDocument();
            }
        }

        public override Date Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        {
            var bsonType = ctx.Reader.GetCurrentBsonType();

            switch (bsonType)
            {
                case BsonType.Document:

                    long ticks = 0;

                    ctx.Reader.ReadStartDocument();
                    while (ctx.Reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        if (ctx.Reader.ReadName() == "Ticks")
                            ticks = ctx.Reader.ReadInt64();
                        else
                            ctx.Reader.SkipValue();
                    }
                    ctx.Reader.ReadEndDocument();

                    return new Date() { DateTime = new DateTime(ticks, DateTimeKind.Utc) };

                case BsonType.Null:
                    ctx.Reader.ReadNull();
                    return null;

                default:
                    throw new FormatException($"Cannot deserialize a 'Date' from a [{bsonType}]");
            }
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            switch (memberName)
            {
                case "Ticks":
                    serializationInfo = new BsonSerializationInfo("Ticks", longSerializer, typeof(long));
                    return true;
                case "DateTime":
                    serializationInfo = new BsonSerializationInfo("DateTime", dtSerializer, typeof(DateTime));
                    return true;
                default:
                    serializationInfo = null;
                    return false;
            }
        }
    }

    /// <summary>
    /// A custom date/time type for precision datetime handling
    /// </summary>
    public class Date
    {
        private long ticks;
        private DateTime date = new DateTime();

        public long Ticks
        {
            get => ticks;
            set { date = new DateTime(value); ticks = value; }
        }

        public DateTime DateTime
        {
            get => date;
            set { date = value; ticks = value.Ticks; }
        }

        public static implicit operator Date(DateTime datetime)
        {
            return new Date { DateTime = datetime };
        }

        public static implicit operator DateTime(Date date)
        {
            if (date == null) throw new NullReferenceException("The [Date] instance is Null!");
            return new DateTime(date.Ticks);
        }
    }
}
