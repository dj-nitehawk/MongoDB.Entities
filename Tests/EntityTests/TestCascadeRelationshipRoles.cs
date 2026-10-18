using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class CascadeRelationshipRoles
{
    [TestMethod]
    [DataRow(false, "entity")]
    [DataRow(true, "entity")]
    [DataRow(false, "id")]
    [DataRow(true, "id")]
    [DataRow(false, "batch")]
    [DataRow(true, "batch")]
    [DataRow(false, "predicate")]
    [DataRow(true, "predicate")]
    [DataRow(false, "transaction")]
    [DataRow(true, "transaction")]
    public async Task overlapping_long_ids_preserve_unrelated_links(bool inverse, string deleteMode)
    {
        var id = DateTime.UtcNow.Ticks;
        await VerifyAsync(
            n => new LongIdParent { ID = id + n }, n => new LongIdChild { ID = id + n },
            p => p.Children, p => p.AllChildren, c => c.AllParents, inverse, deleteMode);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task overlapping_string_ids_preserve_unrelated_links(bool inverse)
    {
        var prefix = Guid.NewGuid().ToString();
        await VerifyAsync(
            n => new StringIdParent { ID = $"{prefix}-{n}" }, n => new StringIdChild { ID = $"{prefix}-{n}" },
            p => p.Children, p => p.AllChildren, c => c.AllParents, inverse, "id");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task overlapping_custom_represented_ids_preserve_unrelated_links(bool inverse)
    {
        var ids = new[] { ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString() };
        await VerifyAsync(
            n => new RepStringIdParent { ID = ids[n - 1] }, n => new RepStringIdChild { ID = ids[n - 1] },
            p => p.Children, p => p.AllChildren, c => c.AllParents, inverse, "predicate");
    }

    static async Task VerifyAsync<TParent, TChild>(Func<int, TParent> makeParent,
                                                  Func<int, TChild> makeChild,
                                                  Func<TParent, Many<TChild, TParent>> children,
                                                  Func<TParent, Many<TChild, TParent>> owners,
                                                  Func<TChild, Many<TParent, TChild>> inverses,
                                                  bool inverse, string deleteMode)
        where TParent : IEntity where TChild : IEntity
    {
        var db = DB.Default;
        var parents = new[] { makeParent(1), makeParent(2), makeParent(3) };
        var kids = new[] { makeChild(1), makeChild(2), makeChild(3) };
        await db.SaveAsync(parents);
        await db.SaveAsync(kids);

        try
        {
            // Crossed equal IDs expose wrong-role matching. The third link is a non-overlapping control.
            await children(parents[0]).AddAsync(kids[1]);
            await children(parents[1]).AddAsync(kids[0]);
            await children(parents[2]).AddAsync(kids[2]);
            await owners(parents[0]).AddAsync(kids[1]);
            await owners(parents[1]).AddAsync(kids[0]);
            await owners(parents[2]).AddAsync(kids[2]);

            if (deleteMode == "transaction")
            {
                using var aborted = db.Transaction();
                if (inverse)
                    await DeleteAsync(aborted, kids[0], "id");
                else
                    await DeleteAsync(aborted, parents[0], "id");
                await aborted.AbortAsync();
                foreach (var parent in parents)
                {
                    Assert.AreEqual(1L, await owners(parent).ChildrenCountAsync(), "Aborted delete changed join records.");
                    Assert.AreEqual(1L, await children(parent).ChildrenCountAsync(), "Aborted delete changed join records.");
                }
            }

            using var transaction = deleteMode == "transaction" ? db.Transaction() : null;
            var deleteDb = transaction ?? db;
            var mode = transaction == null ? deleteMode : "id";
            var result = inverse
                             ? await DeleteAsync(deleteDb, kids[0], mode)
                             : await DeleteAsync(deleteDb, parents[0], mode);
            if (transaction != null)
                await transaction.CommitAsync();
            Assert.AreEqual(1L, result.DeletedCount);

            var survivor = inverse ? 0 : 1;
            var removed = inverse ? 1 : 0;
            Assert.AreEqual(1L, await owners(parents[survivor]).ChildrenCountAsync(), "Unrelated many-to-many link was removed.");
            Assert.AreEqual(0L, await owners(parents[removed]).ChildrenCountAsync(), "Deleted entity still has a many-to-many link.");
            Assert.AreEqual(1L, await children(parents[survivor]).ChildrenCountAsync(), "Unrelated one-to-many link was removed.");
            Assert.AreEqual(0L, await children(parents[removed]).ChildrenCountAsync(), "Deleted entity still has a one-to-many link.");
            Assert.AreEqual(1L, await inverses(kids[inverse ? 1 : 0]).ChildrenCountAsync());
            Assert.AreEqual(1L, await owners(parents[2]).ChildrenCountAsync());
            Assert.AreEqual(1L, await children(parents[2]).ChildrenCountAsync());
            Assert.IsNotNull(await db.Find<TParent>().OneAsync(parents[survivor].GetId()));
            Assert.IsNotNull(await db.Find<TChild>().OneAsync(kids[inverse ? 1 : 0].GetId()));
        }
        finally
        {
            foreach (var parent in parents)
                await db.DeleteAsync(parent);
            foreach (var child in kids)
                await db.DeleteAsync(child);
        }
    }

    static Task<DeleteResult> DeleteAsync<T>(DB db, T entity, string mode) where T : IEntity
        => mode switch
        {
            "entity" => db.DeleteAsync(entity),
            "id" => db.DeleteAsync<T>(entity.GetId()),
            "batch" => db.DeleteAsync<T>(new[] { entity.GetId() }),
            "predicate" => db.DeleteAsync(IdPredicate(entity)),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

    static Expression<Func<T, bool>> IdPredicate<T>(T entity) where T : IEntity
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var id = Expression.Property(parameter, "ID");
        return Expression.Lambda<Func<T, bool>>(Expression.Equal(id, Expression.Constant(entity.GetId(), id.Type)), parameter);
    }

    [TestMethod]
    public async Task self_references_clean_both_roles_and_preserve_other_links()
    {
        var db = DB.Default;
        var nodes = new[] { new SelfNode(), new SelfNode(), new SelfNode() };
        await db.SaveAsync(nodes);
        try
        {
            // Repeated initialization must retain both registrations for the same collection.
            nodes[0].InitOneToMany(() => nodes[0].Children);
            await nodes[0].Children.AddAsync(nodes[1]);
            await nodes[1].Children.AddAsync(nodes[0]);
            await nodes[1].Children.AddAsync(nodes[2]);
            await nodes[0].Outgoing.AddAsync(nodes[1]);
            await nodes[1].Outgoing.AddAsync(nodes[0]);
            await nodes[1].Outgoing.AddAsync(nodes[2]);

            await db.DeleteAsync(nodes[0]);

            Assert.AreEqual(0L, await nodes[0].Children.ChildrenCountAsync());
            Assert.AreEqual(1L, await nodes[1].Children.ChildrenCountAsync());
            Assert.AreEqual(0L, await nodes[0].Outgoing.ChildrenCountAsync());
            Assert.AreEqual(0L, await nodes[0].Incoming.ChildrenCountAsync());
            Assert.AreEqual(1L, await nodes[1].Outgoing.ChildrenCountAsync());
            Assert.AreEqual(1L, await nodes[2].Incoming.ChildrenCountAsync());
        }
        finally
        {
            foreach (var node in nodes)
                await db.DeleteAsync(node);
        }
    }

    public sealed class SelfNode : Entity
    {
        public Many<SelfNode, SelfNode> Children { get; set; }

        [OwnerSide]
        public Many<SelfNode, SelfNode> Outgoing { get; set; }

        [InverseSide]
        public Many<SelfNode, SelfNode> Incoming { get; set; }

        public SelfNode()
        {
            this.InitOneToMany(() => Children);
            this.InitManyToMany(() => Outgoing, n => n.Incoming);
            this.InitManyToMany(() => Incoming, n => n.Outgoing);
        }
    }
}