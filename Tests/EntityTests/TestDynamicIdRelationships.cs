using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DynamicIdRelationships
{
    readonly DB _db = DB.Default;

    /// <summary>
    /// Exercises one-to-many and both sides of many-to-many relationships (queryable + fluent, add/remove
    /// by entity and by raw ID value, children counts and parent lookups) for a given entity ID shape.
    /// </summary>
    async Task VerifyRelationshipsAsync<TParent, TChild>(Func<string, TParent> makeParent,
                                                         Func<string, TChild> makeChild,
                                                         Func<TParent, Many<TChild, TParent>> oneToMany,
                                                         Func<TParent, Many<TChild, TParent>> ownerSide,
                                                         Func<TChild, Many<TParent, TChild>> inverseSide,
                                                         Expression<Func<TChild, string>> childName)
        where TParent : IEntity where TChild : IEntity
    {
        var parent = makeParent("parent");
        var child1 = makeChild("child1");
        var child2 = makeChild("child2");

        await _db.SaveAsync(parent);
        await _db.SaveAsync(child1);
        await _db.SaveAsync(child2);

        // one-to-many: add by entity (twice, to prove upsert de-duplication) and by raw ID value
        await oneToMany(parent).AddAsync(child1);
        await oneToMany(parent).AddAsync(child1);
        await oneToMany(parent).AddAsync(child2.GetId());

        Assert.AreEqual(2, await oneToMany(parent).ChildrenCountAsync());

        var children = await oneToMany(parent)
                             .ChildrenQueryable()
                             .OrderBy(childName)
                             .ToListAsync();

        Assert.HasCount(2, children);
        Assert.AreEqual("child1", childName.Compile()(children[0]));
        Assert.AreEqual("child2", childName.Compile()(children[1]));

        var fluentChildren = await oneToMany(parent).ChildrenFluent().ToListAsync();
        Assert.HasCount(2, fluentChildren);

        var parents = await oneToMany(parent).ParentsQueryable(child1.GetId()).ToListAsync();
        Assert.HasCount(1, parents);
        Assert.AreEqual(parent.GetId(), parents[0].GetId());

        var fluentParents = await oneToMany(parent).ParentsFluent([child2.GetId()]).ToListAsync();
        Assert.HasCount(1, fluentParents);
        Assert.AreEqual(parent.GetId(), fluentParents[0].GetId());

        // remove by entity and by raw ID value
        await oneToMany(parent).RemoveAsync(child1);
        await oneToMany(parent).RemoveAsync(child2.GetId());
        Assert.AreEqual(0, await oneToMany(parent).ChildrenCountAsync());

        // many-to-many: owner side
        await ownerSide(parent).AddAsync([child1, child2]);

        Assert.AreEqual(2, await ownerSide(parent).ChildrenCountAsync());
        Assert.AreEqual(2, await ownerSide(parent).ChildrenQueryable().CountAsync());
        Assert.HasCount(2, await ownerSide(parent).ChildrenFluent().ToListAsync());

        var ownerParents = await ownerSide(parent).ParentsQueryable(child1.GetId()).ToListAsync();
        Assert.HasCount(1, ownerParents);
        Assert.AreEqual(parent.GetId(), ownerParents[0].GetId());

        // many-to-many: inverse side sees the parent as its child
        Assert.AreEqual(1, await inverseSide(child1).ChildrenCountAsync());

        var inverseChildren = await inverseSide(child1).ChildrenQueryable().ToListAsync();
        Assert.HasCount(1, inverseChildren);
        Assert.AreEqual(parent.GetId(), inverseChildren[0].GetId());

        Assert.HasCount(1, await inverseSide(child2).ChildrenFluent().ToListAsync());

        // from the inverse side, the relationship's "parents" are the children linked to the given owner ID
        var inverseParents = await inverseSide(child1).ParentsQueryable(parent.GetId()).ToListAsync();
        Assert.HasCount(2, inverseParents);
        Assert.IsTrue(inverseParents.Any(c => Equals(c.GetId(), child1.GetId())));
        Assert.IsTrue(inverseParents.Any(c => Equals(c.GetId(), child2.GetId())));

        // removal from the inverse side only affects that child's reference
        await inverseSide(child1).RemoveAsync(parent);
        Assert.AreEqual(0, await inverseSide(child1).ChildrenCountAsync());
        Assert.AreEqual(1, await ownerSide(parent).ChildrenCountAsync());

        // cascade delete of the parent must clean up the remaining join records
        await _db.DeleteAsync(parent);
        Assert.AreEqual(0, await inverseSide(child2).ChildrenCountAsync());
    }

    [TestMethod]
    public async Task relationships_with_plain_string_ids()
        => await VerifyRelationshipsAsync<StringIdParent, StringIdChild>(
            n => new() { Name = n },
            n => new() { Name = n },
            p => p.Children,
            p => p.AllChildren,
            c => c.AllParents,
            c => c.Name);

    [TestMethod]
    public async Task relationships_with_custom_format_string_ids()
        => await VerifyRelationshipsAsync<CustomStringIdParent, CustomStringIdChild>(
            n => new() { Name = n },
            n => new() { Name = n },
            p => p.Children,
            p => p.AllChildren,
            c => c.AllParents,
            c => c.Name);

    [TestMethod]
    public async Task relationships_with_long_ids()
        => await VerifyRelationshipsAsync<LongIdParent, LongIdChild>(
            n => new() { Name = n },
            n => new() { Name = n },
            p => p.Children,
            p => p.AllChildren,
            c => c.AllParents,
            c => c.Name);

    [TestMethod]
    public async Task relationships_with_clr_objectid_ids()
        => await VerifyRelationshipsAsync<ObjectIdIdParent, ObjectIdIdChild>(
            n => new() { Name = n },
            n => new() { Name = n },
            p => p.Children,
            p => p.AllChildren,
            c => c.AllParents,
            c => c.Name);

    [TestMethod]
    public async Task relationships_with_guid_ids()
        => await VerifyRelationshipsAsync<GuidIdParent, GuidIdChild>(
            n => new() { Name = n },
            n => new() { Name = n },
            p => p.Children,
            p => p.AllChildren,
            c => c.AllParents,
            c => c.Name);

    [TestMethod]
    public async Task relationships_with_custom_represented_string_ids()
        => await VerifyRelationshipsAsync<RepStringIdParent, RepStringIdChild>(
            n => new() { Name = n },
            n => new() { Name = n },
            p => p.Children,
            p => p.AllChildren,
            c => c.AllParents,
            c => c.Name);

    [TestMethod]
    public async Task join_records_store_the_native_representation_of_custom_represented_string_ids()
    {
        var parent = new RepStringIdParent { Name = "rep parent" };
        var child = new RepStringIdChild { Name = "rep child" };

        await _db.SaveAsync(parent);
        await _db.SaveAsync(child);
        await parent.Children.AddAsync(child);

        var parentId = parent.GetBsonId();
        var join = await parent.Children
                               .JoinQueryable()
                               .Where(j => j.ParentID == parentId)
                               .SingleAsync();

        // the CLR values are strings, but the entities store their _id as ObjectId, so the join
        // record must hold ObjectIds too. storing strings here would make $lookup joins against
        // the entity collections silently return nothing.
        Assert.AreEqual(BsonType.ObjectId, join.ParentID.BsonType);
        Assert.AreEqual(BsonType.ObjectId, join.ChildID.BsonType);
        Assert.AreEqual(ObjectId.Parse(parent.ID), join.ParentID.AsObjectId);
        Assert.AreEqual(ObjectId.Parse(child.ID), join.ChildID.AsObjectId);

        await _db.DeleteAsync(parent);
    }
}
