using MongoDB.Bson.IO;

namespace MongoDB.Entities;

internal class DateSerializer : SerializerBase<Date?>, IBsonDocumentSerializer
{
    private static readonly Int64Serializer _longSerializer = new();
    private static readonly DateTimeSerializer _dtSerializer = new();

    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, Date? date)
    {
        if (date == null)
        {
            ctx.Writer.WriteNull();
        }
        else
        {
            var dtUTC = BsonUtils.ToUniversalTime(date.DateTime);
            ctx.Writer.WriteStartDocument();
            ctx.Writer.WriteDateTime(nameof(Date.DateTime), BsonUtils.ToMillisecondsSinceEpoch(dtUTC));
            ctx.Writer.WriteInt64(nameof(Date.Ticks), dtUTC.Ticks);
            ctx.Writer.WriteEndDocument();
        }
    }

    public override Date? Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
    {
        var bsonType = ctx.Reader.GetCurrentBsonType();

        switch (bsonType)
        {
            case BsonType.Document:

                long ticks = 0;

                ctx.Reader.ReadStartDocument();
                while (ctx.Reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    if (ctx.Reader.ReadName() == nameof(Date.Ticks))
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

    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo? serializationInfo)
    {
        switch (memberName)
        {
            case nameof(Date.Ticks):
                serializationInfo = new BsonSerializationInfo(nameof(Date.Ticks), _longSerializer, typeof(long));
                return true;
            case nameof(Date.DateTime):
                serializationInfo = new BsonSerializationInfo(nameof(Date.DateTime), _dtSerializer, typeof(DateTime));
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
    private long _ticks;
    private DateTime _date = new();

    public long Ticks
    {
        get => _ticks;
        set { _date = new DateTime(value); _ticks = value; }
    }

    public DateTime DateTime
    {
        get => _date;
        set { _date = value; _ticks = value.Ticks; }
    }

    public static implicit operator Date(DateTime datetime)
    {
        return new Date { DateTime = datetime };
    }

    public static implicit operator DateTime(Date date)
    {
        return new DateTime(date.Ticks);
    }
}
