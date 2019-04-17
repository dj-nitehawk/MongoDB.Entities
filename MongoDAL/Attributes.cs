using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDAL
{
    /// <summary>
    /// Indicates that this field or property should be ignored when this class is persisted to MongoDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MongoIgnoreAttribute : BsonIgnoreAttribute { }

    /// <summary>
    /// Specifies whether extra elements should be ignored when this class is deserialized
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MongoIgnoreExtrasAttribute : BsonIgnoreExtraElementsAttribute { }
}
