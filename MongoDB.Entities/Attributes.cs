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
    public class OwnerSide : Attribute { }

    /// <summary>
    /// Indicates that this property is the inverse side of a many-to-many relationship
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InverseSide : Attribute { }

	/// <summary>
	/// Allows user to specify a different collection name to an entity
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class Collection : Attribute {
		public string Name { get; set; }
	}
}
