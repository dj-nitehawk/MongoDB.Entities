namespace MongoDB.Entities;


/// <summary>
/// Represents a parent-child relationship between two entities.
/// <para>TIP: The ParentID and ChildID switches around for many-to-many relationships depending on the side of the relationship you're accessing.</para>
/// </summary>
public class JoinRecord : Entity
{
    /// <summary>
    /// The ID of the parent IEntity for both one-to-many and the owner side of many-to-many relationships.
    /// </summary>    
    public string ParentID { get; set; } = null!;

    /// <summary>
    /// The ID of the child IEntity in one-to-many relationships and the ID of the inverse side IEntity in many-to-many relationships.
    /// </summary>    
    public string ChildID { get; set; } = null!;
}

/// <summary>
/// Represents a parent-child relationship between two entities.
/// <para>TIP: The ParentID and ChildID switches around for many-to-many relationships depending on the side of the relationship you're accessing.</para>
/// </summary>
public class JoinRecord<TParentId, TChildId> : Entity<(TParentId ParentId, TChildId ChildId)>
{
    public JoinRecord(TParentId parentID, TChildId childID)
    {
        ID = (parentID, childID);
    }

    /// <summary>
    /// The ID of the parent IEntity for both one-to-many and the owner side of many-to-many relationships.
    /// </summary>    
    [BsonIgnore]
    public TParentId ParentID => ID.ParentId;

    /// <summary>
    /// The ID of the child IEntity in one-to-many relationships and the ID of the inverse side IEntity in many-to-many relationships.
    /// </summary>    
    [BsonIgnore]
    public TChildId ChildID => ID.ChildId;

    public override (TParentId ParentId, TChildId ChildId) GenerateNewID()
    {
        throw new NotImplementedException();
    }
}
