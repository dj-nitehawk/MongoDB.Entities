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
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreAttribute : BsonIgnoreAttribute { }

    /// <summary>
    /// Specifies the field name and/or the order of the persisted document.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FieldAttribute : BsonElementAttribute
    {
        public FieldAttribute(int fieldOrder) { Order = fieldOrder; }
        public FieldAttribute(string fieldName) : base(fieldName) { }
        public FieldAttribute(string fieldName, int fieldOrder) : base(fieldName) { Order = fieldOrder; }
    }

    /// <summary>
    /// Indicates that this property is the owner side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OwnerSideAttribute : Attribute { }

    /// <summary>
    /// Indicates that this property is the inverse side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class InverseSideAttribute : Attribute { }

    //todo: remove this attribute in the next major version jump
    [Obsolete("Please use the [Collection(\"...\")] attribute instead")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NameAttribute : CollectionAttribute
    {
        public NameAttribute(string name) : base(name) { }
    }

    /// <summary>
    /// Use this attribute to specify a custom MongoDB collection name for an IEntity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CollectionAttribute : Attribute
    {
        public string Name { get; }

        /// <summary>
        /// Use this attribute to specify a custom MongoDB collection name for an IEntity.
        /// </summary>
        /// <param name="name">The name you want to use for the collection</param>
        public CollectionAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
        }
    }

    /// <summary>
    /// Use this attribute on properties that you want to omit when using SavePreserving() instead of supplying an expression. 
    /// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving() 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PreserveAttribute : Attribute { }

    /// <summary>
    /// Properties that don't have this attribute will be omitted when using SavePreserving()
    /// TIP: These attribute decorations are only effective if you do not specify a preservation expression when calling SavePreserving()
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DontPreserveAttribute : Attribute { }

    /// <summary>
    /// Use this attribute to mark a property in order to save it in MongoDB server as ObjectId
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ObjectIdAttribute : BsonRepresentationAttribute
    {
        public ObjectIdAttribute() : base(BsonType.ObjectId)
        { }
    }

    /// <summary>
    /// Use this attribute to mark a string property to store the value in MongoDB as ObjectID if it is a valid ObjectId string. 
    /// If it is not a valid ObjectId string, it will be stored as string. This is useful when using custom formats for the ID field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AsObjectIdAttribute : BsonSerializerAttribute
    {
        public AsObjectIdAttribute() : base(typeof(ObjectIdSerializer)) { }

        private class ObjectIdSerializer : SerializerBase<string>
        {
            public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, string value)
            {
                if (value == null)
                {
                    ctx.Writer.WriteNull();
                    return;
                }

                if (value.Length == 24 && ObjectId.TryParse(value, out var oID))
                {
                    ctx.Writer.WriteObjectId(oID);
                    return;
                }

                ctx.Writer.WriteString(value);
            }

            public override string Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
            {
                switch (ctx.Reader.CurrentBsonType)
                {
                    case BsonType.String:
                        return ctx.Reader.ReadString();

                    case BsonType.ObjectId:
                        return ctx.Reader.ReadObjectId().ToString();

                    case BsonType.Null:
                        ctx.Reader.ReadNull();
                        return null;

                    default:
                        throw new BsonSerializationException($"'{ctx.Reader.CurrentBsonType}' values are not valid on properties decorated with an [AsObjectId] attribute!");
                }
            }
        }
    }
}
