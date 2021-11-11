using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities;

/// <summary>
/// Represents a parent-child relationship between two entities.
/// <para>TIP: The ParentID and ChildID switches around for many-to-many relationships depending on the side of the relationship you're accessing.</para>
/// </summary>
public class JoinRecord<TId1, TId2> : Entity<(TId1 ParentID, TId2 ChildID)>
{
    ///// <summary>
    ///// The ID of the parent IEntity for both one-to-many and the owner side of many-to-many relationships.
    ///// </summary>
    //[BsonIgnore]
    //public string ParentID { get; set; } = null!;

    ///// <summary>
    ///// The ID of the child IEntity in one-to-many relationships and the ID of the inverse side IEntity in many-to-many relationships.
    ///// </summary>
    //[AsObjectId]
    //public string ChildID { get; set; } = null!;


    public override (TId1 ParentID, TId2 ChildID) GenerateNewID()
    {
        //the caller is responsible for assigning ids
        throw new NotImplementedException();
    }
}
