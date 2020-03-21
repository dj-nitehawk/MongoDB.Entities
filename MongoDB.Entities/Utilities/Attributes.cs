using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities
{
    /// <summary>
    /// Indicates that this property should be ignored when this class is persisted to MongoDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnoreAttribute : BsonIgnoreAttribute { }

    /// <summary>
    /// Indicates that this property is the owner side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class OwnerSideAttribute : Attribute { }

    /// <summary>
    /// Indicates that this property is the inverse side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
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
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            Name = name;
        }
    }

    /// <summary>
    /// Use this attribute to specify the database to store this IEntity in.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DatabaseAttribute : Attribute
    {
        public string Name { get; }

        /// <summary>
        /// Use this attribute to specify the database to store this IEntity in.
        /// </summary>
        /// <param name = "name" > The name you want to use for the collection</param>
        public DatabaseAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
        }
    }
}
