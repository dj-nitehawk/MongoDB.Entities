using System;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests.Models;

// parent/child entity pairs with different ID types and representations for verifying
// that one-to-many and many-to-many relationships work with any entity ID shape.

#region plain string IDs

[Collection("StringIdParent")]
public class StringIdParent : IEntity
{
    [BsonId]
    public string ID { get; set; }

    public string Name { get; set; }

    public Many<StringIdChild, StringIdParent> Children { get; set; }

    [OwnerSide]
    public Many<StringIdChild, StringIdParent> AllChildren { get; set; }

    public StringIdParent()
    {
        this.InitOneToMany(() => Children);
        this.InitManyToMany(() => AllChildren, c => c.AllParents);
    }

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

[Collection("StringIdChild")]
public class StringIdChild : IEntity
{
    [BsonId]
    public string ID { get; set; }

    public string Name { get; set; }

    [InverseSide]
    public Many<StringIdParent, StringIdChild> AllParents { get; set; }

    public StringIdChild()
    {
        this.InitManyToMany(() => AllParents, p => p.AllChildren);
    }

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

#endregion

#region custom format string IDs

[Collection("CustomStringIdParent")]
public class CustomStringIdParent : IEntity
{
    [BsonId]
    public string ID { get; set; }

    public string Name { get; set; }

    public Many<CustomStringIdChild, CustomStringIdParent> Children { get; set; }

    [OwnerSide]
    public Many<CustomStringIdChild, CustomStringIdParent> AllChildren { get; set; }

    public CustomStringIdParent()
    {
        this.InitOneToMany(() => Children);
        this.InitManyToMany(() => AllChildren, c => c.AllParents);
    }

    public object GenerateNewID()
        => $"parent-{Guid.NewGuid():N}";

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

[Collection("CustomStringIdChild")]
public class CustomStringIdChild : IEntity
{
    [BsonId]
    public string ID { get; set; }

    public string Name { get; set; }

    [InverseSide]
    public Many<CustomStringIdParent, CustomStringIdChild> AllParents { get; set; }

    public CustomStringIdChild()
    {
        this.InitManyToMany(() => AllParents, p => p.AllChildren);
    }

    public object GenerateNewID()
        => $"child-{Guid.NewGuid():N}";

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

#endregion

#region long IDs

[Collection("LongIdParent")]
public class LongIdParent : IEntity
{
    static long _idCounter = DateTime.UtcNow.Ticks;

    [BsonId]
    public long ID { get; set; }

    public string Name { get; set; }

    public Many<LongIdChild, LongIdParent> Children { get; set; }

    [OwnerSide]
    public Many<LongIdChild, LongIdParent> AllChildren { get; set; }

    public LongIdParent()
    {
        this.InitOneToMany(() => Children);
        this.InitManyToMany(() => AllChildren, c => c.AllParents);
    }

    public object GenerateNewID()
        => Interlocked.Increment(ref _idCounter);

    public bool HasDefaultID()
        => ID == 0;
}

[Collection("LongIdChild")]
public class LongIdChild : IEntity
{
    static long _idCounter = DateTime.UtcNow.Ticks;

    [BsonId]
    public long ID { get; set; }

    public string Name { get; set; }

    [InverseSide]
    public Many<LongIdParent, LongIdChild> AllParents { get; set; }

    public LongIdChild()
    {
        this.InitManyToMany(() => AllParents, p => p.AllChildren);
    }

    public object GenerateNewID()
        => Interlocked.Increment(ref _idCounter);

    public bool HasDefaultID()
        => ID == 0;
}

#endregion

#region CLR ObjectId IDs

[Collection("ObjectIdIdParent")]
public class ObjectIdIdParent : ObjectIdEntity
{
    public string Name { get; set; }

    public Many<ObjectIdIdChild, ObjectIdIdParent> Children { get; set; }

    [OwnerSide]
    public Many<ObjectIdIdChild, ObjectIdIdParent> AllChildren { get; set; }

    public ObjectIdIdParent()
    {
        this.InitOneToMany(() => Children);
        this.InitManyToMany(() => AllChildren, c => c.AllParents);
    }
}

[Collection("ObjectIdIdChild")]
public class ObjectIdIdChild : ObjectIdEntity
{
    public string Name { get; set; }

    [InverseSide]
    public Many<ObjectIdIdParent, ObjectIdIdChild> AllParents { get; set; }

    public ObjectIdIdChild()
    {
        this.InitManyToMany(() => AllParents, p => p.AllChildren);
    }
}

#endregion

#region Guid IDs

[Collection("GuidIdParent")]
public class GuidIdParent : IEntity
{
    [BsonId]
    public Guid ID { get; set; }

    public string Name { get; set; }

    public Many<GuidIdChild, GuidIdParent> Children { get; set; }

    [OwnerSide]
    public Many<GuidIdChild, GuidIdParent> AllChildren { get; set; }

    public GuidIdParent()
    {
        this.InitOneToMany(() => Children);
        this.InitManyToMany(() => AllChildren, c => c.AllParents);
    }

    public object GenerateNewID()
        => Guid.NewGuid();

    public bool HasDefaultID()
        => ID == Guid.Empty;
}

[Collection("GuidIdChild")]
public class GuidIdChild : IEntity
{
    [BsonId]
    public Guid ID { get; set; }

    public string Name { get; set; }

    [InverseSide]
    public Many<GuidIdParent, GuidIdChild> AllParents { get; set; }

    public GuidIdChild()
    {
        this.InitManyToMany(() => AllParents, p => p.AllChildren);
    }

    public object GenerateNewID()
        => Guid.NewGuid();

    public bool HasDefaultID()
        => ID == Guid.Empty;
}

#endregion

#region custom-represented string IDs (stored as ObjectId)

[Collection("RepStringIdParent")]
public class RepStringIdParent : IEntity
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string ID { get; set; }

    public string Name { get; set; }

    public Many<RepStringIdChild, RepStringIdParent> Children { get; set; }

    [OwnerSide]
    public Many<RepStringIdChild, RepStringIdParent> AllChildren { get; set; }

    public RepStringIdParent()
    {
        this.InitOneToMany(() => Children);
        this.InitManyToMany(() => AllChildren, c => c.AllParents);
    }

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

[Collection("RepStringIdChild")]
public class RepStringIdChild : IEntity
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string ID { get; set; }

    public string Name { get; set; }

    [InverseSide]
    public Many<RepStringIdParent, RepStringIdChild> AllParents { get; set; }

    public RepStringIdChild()
    {
        this.InitManyToMany(() => AllParents, p => p.AllChildren);
    }

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

#endregion
