using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public class AuditHookTests
{
    [TestMethod]
    [DataRow("save")]
    [DataRow("save-batch")]
    [DataRow("insert")]
    [DataRow("insert-batch")]
    [DataRow("only")]
    [DataRow("only-batch")]
    [DataRow("except")]
    [DataRow("except-batch")]
    [DataRow("preserving")]
    [DataRow("replace")]
    public async Task audit_views_preserve_save_hooks(string operation)
    {
        await WithDatabase(async (db, source) =>
        {
            var view = source.WithModifiedBy(User("view"));
            var entities = new[] { new AuditHookEntity(), new AuditHookEntity() };
            if (operation is "preserving" or "replace")
                await db.WithModifiedBy(User("seed")).SaveAsync(entities);

            switch (operation)
            {
                case "save": await view.SaveAsync(entities[0]); break;
                case "save-batch": await view.SaveAsync(entities); break;
                case "insert": await view.InsertAsync(entities[0]); break;
                case "insert-batch": await view.InsertAsync(entities); break;
                case "only": await view.SaveOnlyAsync(entities[0], e => new { e.SaveCalls, e.SaveUser, e.ModifiedBy }); break;
                case "only-batch": await view.SaveOnlyAsync(entities, e => new { e.SaveCalls, e.SaveUser, e.ModifiedBy }); break;
                case "except": await view.SaveExceptAsync(entities[0], e => new { e.Name }); break;
                case "except-batch": await view.SaveExceptAsync(entities, e => new { e.Name }); break;
                case "preserving": await view.SavePreservingAsync(entities[0]); break;
                case "replace": await view.Replace<AuditHookEntity>().MatchID(entities[0].ID).WithEntity(entities[0]).ExecuteAsync(); break;
            }

            foreach (var entity in operation.EndsWith("batch") ? entities : entities.Take(1))
                AssertSaved(await db.Find<AuditHookEntity>().OneAsync(entity.ID), "view");
            Assert.AreEqual("source", source.ModifiedBy?.UserID);
        });
    }

    [TestMethod]
    [DataRow("update")]
    [DataRow("queue")]
    [DataRow("get")]
    public async Task audit_views_preserve_update_hooks(string operation)
    {
        await WithDatabase(async (db, source) =>
        {
            var entity = new AuditHookEntity();
            await db.WithModifiedBy(User("seed")).SaveAsync(entity);
            var view = source.WithModifiedBy(User("view"));
            if (operation == "get")
            {
                var result = await view.UpdateAndGet<AuditHookEntity>().MatchID(entity.ID).Modify(e => e.Name, "updated").ExecuteAsync();
                Assert.AreEqual("view/view", result?.UpdateUser);
            }
            else
            {
                var update = view.Update<AuditHookEntity>().MatchID(entity.ID).Modify(e => e.Name, "updated");
                if (operation == "queue")
                    update.AddToQueue();
                await update.ExecuteAsync();
            }
            var saved = await db.Find<AuditHookEntity>().OneAsync(entity.ID);
            Assert.IsNotNull(saved);
            Assert.AreEqual(1, saved.UpdateCalls);
            Assert.AreEqual("view/view", saved.UpdateUser);
            Assert.AreEqual("view", saved.ModifiedBy.UserID);
            Assert.AreEqual("source", source.ModifiedBy?.UserID);
        });
    }

    [TestMethod]
    public async Task audit_views_reject_invalid_writes_before_persistence()
    {
        await WithDatabase(async (db, source) =>
        {
            var view = source.WithModifiedBy(User("reject"));
            var entity = new AuditHookEntity();
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => view.SaveAsync(entity));
            Assert.AreEqual(0L, await db.CountAsync<AuditHookEntity>());
            await db.WithModifiedBy(User("seed")).SaveAsync(entity);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                view.Update<AuditHookEntity>().MatchID(entity.ID).Modify(e => e.Name, "invalid").ExecuteAsync());
            var saved = await db.Find<AuditHookEntity>().OneAsync(entity.ID);
            Assert.AreEqual("original", saved?.Name);
            Assert.AreEqual("seed", saved?.ModifiedBy.UserID);
        });
    }

    [TestMethod]
    public async Task chained_and_concurrent_views_keep_their_own_hook_audit_context()
    {
        await WithDatabase(async (db, source) =>
        {
            var first = source.WithModifiedBy(User("first"));
            var second = first.WithModifiedBy(User("second"));
            var a = new AuditHookEntity();
            var b = new AuditHookEntity();
            await Task.WhenAll(first.SaveAsync(a), second.SaveAsync(b));
            // Resolve both builders before executing either, so captured contexts must stay distinct.
            var updateA = first.Update<AuditHookEntity>().MatchID(a.ID).Modify(e => e.Name, "a");
            var updateB = second.Update<AuditHookEntity>().MatchID(b.ID).Modify(e => e.Name, "b");
            await Task.WhenAll(updateA.ExecuteAsync(), updateB.ExecuteAsync());
            AssertSaved(await db.Find<AuditHookEntity>().OneAsync(a.ID), "first", 1);
            AssertSaved(await db.Find<AuditHookEntity>().OneAsync(b.ID), "second", 1);
            var original = new AuditHookEntity();
            await source.SaveAsync(original);
            AssertSaved(await db.Find<AuditHookEntity>().OneAsync(original.ID), "source");
            Assert.AreEqual("source", source.ModifiedBy?.UserID);
            Assert.AreEqual("first", first.ModifiedBy?.UserID);
        });
    }

    [TestMethod]
    [DataRow("commit")]
    [DataRow("abort")]
    [DataRow("dispose")]
    public async Task transaction_audit_views_preserve_hooks_without_owning_the_session(string outcome)
    {
        await WithDatabase(async (db, source) =>
        {
            var entity = new AuditHookEntity();
            using (var transaction = source.WithModifiedBy(User("transaction")).Transaction())
            {
                var view = transaction.WithModifiedBy(User("view")).WithModifiedBy(User("chained"));
                Assert.IsFalse(view is IDisposable);
                await view.SaveAsync(entity);
                await view.Update<AuditHookEntity>().MatchID(entity.ID).Modify(e => e.Name, "updated").ExecuteAsync();
                AssertSaved(await view.Find<AuditHookEntity>().OneAsync(entity.ID), "chained", 1);
                Assert.AreEqual(1L, (await view.Find<AuditHookEntity>().OneAsync(entity.ID))?.HookReadCount);
                Assert.IsNull(await db.Find<AuditHookEntity>().OneAsync(entity.ID));
                if (outcome == "commit")
                    await transaction.CommitAsync();
                else if (outcome == "abort")
                    await transaction.AbortAsync();
            }
            var saved = await db.Find<AuditHookEntity>().OneAsync(entity.ID);
            if (outcome == "commit")
                AssertSaved(saved, "chained", 1);
            else
                Assert.IsNull(saved);
            Assert.AreEqual("source", source.ModifiedBy?.UserID);
        });
    }

    static ModifiedBy User(string id) => new() { UserID = id };

    [TestMethod]
    public async Task audit_view_retains_overrides_when_derived_from_an_existing_view()
    {
        await WithDatabase(async (db, source) =>
        {
            var derived = new OverrideHookDb(source.WithModifiedBy(User("first")));
            var view = derived.WithModifiedBy(User("second"));
            var entity = new AuditHookEntity();
            await view.SaveAsync(entity);
            var saved = await db.Find<AuditHookEntity>().OneAsync(entity.ID);
            AssertSaved(saved, "second");
            Assert.AreEqual("derived", saved?.Name);
        });
    }

    static void AssertSaved(AuditHookEntity? saved, string user, int updateCalls = 0)
    {
        Assert.IsNotNull(saved);
        Assert.AreEqual(1, saved.SaveCalls);
        Assert.AreEqual(user + "/" + user, saved.SaveUser);
        Assert.AreEqual(user, saved.ModifiedBy.UserID);
        Assert.AreEqual(updateCalls, saved.UpdateCalls);
        if (updateCalls != 0)
            Assert.AreEqual(user + "/" + user, saved.UpdateUser);
    }

    static async Task WithDatabase(Func<DB, HookDb, Task> test)
    {
        var db = await InitTest.InitTestDatabase("audit-hooks-" + Guid.NewGuid().ToString("N"));
        try
        {
            await db.Database().CreateCollectionAsync(nameof(AuditHookEntity));
            await test(db, new HookDb(db.WithModifiedBy(User("source"))));
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.DatabaseName());
        }
    }

    public class AuditHookEntity : Entity
    {
        [Preserve] public string Name { get; set; } = "original";
        public ModifiedBy ModifiedBy { get; set; } = new();
        public int SaveCalls { get; set; }
        public string SaveUser { get; set; } = "";
        public int UpdateCalls { get; set; }
        public string UpdateUser { get; set; } = "";
        public long HookReadCount { get; set; }
    }

    public class HookDb(DB source) : DB(source)
    {
        protected override Action<T>? OnBeforeSave<T>()
        {
            var factoryUser = ModifiedBy?.UserID;
            return entity =>
            {
                if (ModifiedBy?.UserID == "reject")
                    throw new InvalidOperationException("Rejected by save hook");
                if (entity is AuditHookEntity audit)
                {
                    audit.SaveCalls++;
                    audit.SaveUser = factoryUser + "/" + ModifiedBy?.UserID;
                }
            };
        }

        protected override Action<UpdateBase<T>>? OnBeforeUpdate<T>()
        {
            var factoryUser = ModifiedBy?.UserID;
            return update =>
            {
                if (ModifiedBy?.UserID == "reject")
                    throw new InvalidOperationException("Rejected by update hook");
                if (update is UpdateBase<AuditHookEntity> audit)
                {
                    audit.AddModification(b => b.Inc(e => e.UpdateCalls, 1));
                    audit.AddModification(e => e.UpdateUser, factoryUser + "/" + ModifiedBy?.UserID);
                    // A sessionless hook read cannot see an uncommitted save.
                    audit.AddModification(e => e.HookReadCount, CountAsync<AuditHookEntity>().GetAwaiter().GetResult());
                }
            };
        }
    }

    public class OverrideHookDb(DB source) : HookDb(source)
    {
        protected override Action<T>? OnBeforeSave<T>()
        {
            var hook = base.OnBeforeSave<T>();
            return entity =>
            {
                hook?.Invoke(entity);
                if (entity is AuditHookEntity audit)
                    audit.Name = "derived";
            };
        }
    }
}
