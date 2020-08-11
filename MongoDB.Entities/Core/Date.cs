using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Entities
{
    internal class DateSerializer : SerializerBase<Date>, IBsonDocumentSerializer
    {
        private static readonly BsonDocumentSerializer docSerializer = new BsonDocumentSerializer();
        private static readonly Int64Serializer longSerializer = new Int64Serializer();
        private static readonly DateTimeSerializer dtSerializer = new DateTimeSerializer();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Date date)
        {
            if (date == null)
            {
                context.Writer.WriteStartDocument();
                context.Writer.WriteNull("DateTime");
                context.Writer.WriteInt64("Ticks", 0);
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
            var ticks = docSerializer.Deserialize(context, args)
                                     .GetValue("Ticks").AsInt64;
            return
                (ticks == 0) ?
                null :
                new Date() { DateTime = new DateTime(ticks, DateTimeKind.Utc) };
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
