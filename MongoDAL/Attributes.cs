using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDAL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MongoIgnoreAttribute : BsonIgnoreAttribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MongoIgnoreExtrasAttribute : BsonIgnoreExtraElementsAttribute { }
}
