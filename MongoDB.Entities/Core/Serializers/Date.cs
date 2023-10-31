using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Globalization;

namespace MongoDB.Entities;

class DateSerializer : SerializerBase<Date>, IBsonDocumentSerializer
{
    static readonly IBsonSerializer<long> _longSerializer = BsonSerializer.LookupSerializer<long>();
    static readonly IBsonSerializer<DateTime> _dtSerializer = BsonSerializer.LookupSerializer<DateTime>();

    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, Date date)
    {
        if (date == null)
            ctx.Writer.WriteNull();
        else
        {
            var dtUtc = BsonUtils.ToUniversalTime(date.DateTime);
            ctx.Writer.WriteStartDocument();
            ctx.Writer.WriteDateTime("DateTime", BsonUtils.ToMillisecondsSinceEpoch(dtUtc));
            ctx.Writer.WriteInt64("Ticks", dtUtc.Ticks);
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

                return new() { DateTime = new(ticks, DateTimeKind.Utc) };

            case BsonType.Null:
                ctx.Reader.ReadNull();

                return null!;

            default:
                throw new FormatException($"Cannot deserialize a 'Date' from a [{bsonType}]");
        }
    }

    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        switch (memberName)
        {
            case "Ticks":
                serializationInfo = new("Ticks", _longSerializer, typeof(long));

                return true;
            case "DateTime":
                serializationInfo = new("DateTime", _dtSerializer, typeof(DateTime));

                return true;
            default:
                serializationInfo = null!;

                return false;
        }
    }
}

/// <summary>
/// A custom date/time type for precision datetime handling
/// </summary>
public class Date
{
    long _ticks;
    DateTime _date;

    public long Ticks
    {
        get => _ticks;
        set
        {
            _date = new(value);
            _ticks = value;
        }
    }

    public DateTime DateTime
    {
        get => _date;
        set
        {
            _date = value;
            _ticks = value.Ticks;
        }
    }

    public Date() { }

    /// <summary>
    /// instantiate a Date with ticks
    /// </summary>
    /// <param name="ticks">the ticks</param>
    public Date(long ticks) { Ticks = ticks; }

    /// <summary>
    /// instantiate a Date with a DateTime
    /// </summary>
    /// <param name="dateTime">the DateTime</param>
    public Date(DateTime dateTime) { DateTime = dateTime; }

    public override string ToString()
        => _date.ToString(CultureInfo.InvariantCulture);
}