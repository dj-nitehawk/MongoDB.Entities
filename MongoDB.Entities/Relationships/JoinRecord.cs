using MongoDB.Bson;

namespace MongoDB.Entities;

/// <summary>
/// Represents a parent-child relationship between two entities.
/// <para>TIP: The ParentID and ChildID switches around for many-to-many relationships depending on the side of the relationship you're accessing.</para>
/// </summary>
public class JoinRecord : ObjectIdEntity
{
    /// <summary>
    /// The ID of the parent IEntity for both one-to-many and the owner side of many-to-many relationships.
    /// <para>Holds the stored (serialized) representation of the parent entity's ID, whatever type that may be.</para>
    /// </summary>
    public BsonValue ParentID { get; set; } = null!;

    /// <summary>
    /// The ID of the child IEntity in one-to-many relationships and the ID of the inverse side IEntity in many-to-many relationships.
    /// <para>Holds the stored (serialized) representation of the child entity's ID, whatever type that may be.</para>
    /// </summary>
    public BsonValue ChildID { get; set; } = null!;
}
