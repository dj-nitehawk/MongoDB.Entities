using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace MongoDB.Entities
{
    /// <summary>
    /// Indicates that this property should be ignored when this class is persisted to MongoDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : BsonIgnoreAttribute { }

    /// <summary>
    /// Indicates that this property is the owner side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OwnerSideAttribute : Attribute { }

    /// <summary>
    /// Indicates that this property is the inverse side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InverseSideAttribute : Attribute { }

    /// <summary>
    /// Use this attribute to specify a custom MongoDB collection name for an IEntity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NameAttribute : Attribute
    {
        public string Name { get; }

        /// <summary>
        /// Use this attribute to specify a custom MongoDB collection name for an IEntity.
        /// </summary>
        /// <param name="name">The name you want to use for the collection</param>
        public NameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
        }
    }

    /// <summary>
    /// Use this attribute on properties that you want to omit when using SavePreserving() instead of supplying an expression. 
    /// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving() 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PreserveAttribute : Attribute { }

    /// <summary>
    /// Properties that don't have this attribute will be omitted when using SavePreserving()
    /// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving()
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DontPreserveAttribute : Attribute { }

    /// <summary>
    /// Use this attribute to mark a property in order to save it in MongoDB server as ObjectId
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ObjectIdAttribute : BsonRepresentationAttribute
    {
        public ObjectIdAttribute() : base(BsonType.ObjectId)
        { }
    }

    /// <summary>
    /// Use this attribute to mark a string property to store the value in MongoDB as ObjectID if it is a valid ObjectId string. 
    /// If it is not a valid ObjectId string, it will be stored as string. This is useful when using custom formats for the ID field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AsObjectIdAttribute : BsonSerializerAttribute
    {
        public AsObjectIdAttribute() : base(typeof(IDSerializer)) { }

        private class IDSerializer : SerializerBase<string>
        {
            public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, string value)
            {
                if (value?.Length == 24 && ObjectId.TryParse(value, out var oId))
                {
                    ctx.Writer.WriteObjectId(oId);
                    return;
                }

                ctx.Writer.WriteString(value);
            }

            public override string Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
            {
                if (ctx.Reader.CurrentBsonType == BsonType.ObjectId)
                    return ctx.Reader.ReadObjectId().ToString();

                return ctx.Reader.ReadString();
            }
        }
    }
}
