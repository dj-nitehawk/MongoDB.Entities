using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDAL
{
    /// <summary>
    /// Indicates that this property should be ignored when this class is persisted to MongoDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MongoIgnoreAttribute : BsonIgnoreAttribute { }

    /// <summary>
    /// Indicates that this property is a reference to one or more entities in MongoDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MongoRefAttribute : BsonRepresentationAttribute
    {
        public MongoRefAttribute() : base(BsonType.ObjectId) { }
    }
}
