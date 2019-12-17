using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Entities
{
    internal class DateSerializer : SerializerBase<Date>, IBsonDocumentSerializer
    {
        private static readonly BsonDocumentSerializer serializer = new BsonDocumentSerializer();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Date date)
        {
            if (date == null)
            {
                context.Writer.WriteStartDocument();
                context.Writer.WriteNull("DateTime");
                context.Writer.WriteInt64("Ticks",0);
                context.Writer.WriteEndDocument();
                return;
            }

            var dtUTC = BsonUtils.ToUniversalTime(date.DateTime);
            context.Writer.WriteStartDocument();
            context.Writer.WriteDateTime("DateTime", BsonUtils.ToMillisecondsSinceEpoch(dtUTC));
            context.Writer.WriteInt64("Ticks", dtUTC.Ticks);
            context.Writer.WriteEndDocument();
        }

        public override Date Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var ticks = serializer.Deserialize(context, args)
                                  .GetValue("Ticks").AsInt64;

            if (ticks == 0) return null;

            return new Date()
            {
                DateTime = new DateTime(ticks, DateTimeKind.Utc)
            };
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            switch (memberName)
            {
                case "Ticks":
                    serializationInfo = new BsonSerializationInfo("Ticks", new Int64Serializer(), typeof(long));
                    return true;
                case "DateTime":
                    serializationInfo = new BsonSerializationInfo("DateTime", new DateTimeSerializer(), typeof(DateTime));
                    return true;
                default:
                    serializationInfo = null;
                    return false;
            }
        }
    }

    public class Date
    {
        private long ticks = 0;
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

        public static implicit operator Date(DateTime dt)
        {
            return new Date { DateTime = dt };
        }

        public static implicit operator DateTime(Date dt)
        {
            if (dt == null) throw new NullReferenceException("The [Date] instance is Null!");
            return new DateTime(dt.Ticks);
        }
    }
}
