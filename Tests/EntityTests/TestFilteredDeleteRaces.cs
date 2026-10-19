using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Entities.Tests;

[TestClass]
public class FilteredDeleteRaces
{
    public class Record : IEntity
    {
        [BsonId, AsObjectId]
        public string ID { get; set; } = ObjectId.GenerateNewId().ToString();
        public bool Active { get; set; } = true;
    }

    public class Parent : Record
    {
        public Many<Child, Parent> Children { get; set; } = null!;
    }

    public class Child : Record;

    public class GuidParent : IEntity
    {
        [BsonId]
        public Guid ID { get; set; } = Guid.NewGuid();
        public bool Active { get; set; } = true;
        public Many<Child, GuidParent> Children { get; set; } = null!;
    }

    public class Unrelated : Record { }

    public class LateParent : Record
    {
        public Many<Child, LateParent> Children { get; set; } = null!;
    }

    class FilteredDb : DB
    {
        public FilteredDb(DB source) : base(source)
        {
            SetGlobalFilter<Parent>(p => p.Active);
            SetGlobalFilter<Child>(c => c.Active);
            SetGlobalFilter<GuidParent>(p => p.Active);
            SetGlobalFilter<Unrelated>(p => p.Active);
            SetGlobalFilter<LateParent>(p => p.Active);
        }
    }

    static async Task<DB> Monitored<T>(DB db, Action<CommandStartedEvent> onDelete) where T : IEntity
    {
        var settings = db.Database().Client.Settings.Clone();
        settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
        {
            if (command.CommandName is "delete" or "findAndModify" &&
                command.Command[command.CommandName] == db.CollectionName<T>())
                onDelete(command);
        });
        return new FilteredDb(await DB.InitAsync(db.Database().DatabaseNamespace.DatabaseName, settings));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public Task unrelated_filtered_batch_uses_one_bulk_delete_and_rechecks_filter(bool predicate, bool allExcluded)
        => Isolated(async db =>
        {
            var survivor = new Unrelated();
            var removed = new Unrelated();
            var excluded = new Unrelated { Active = false };
            await db.SaveAsync(new[] { survivor, removed, excluded });
            var bulk = 0;
            var conditional = 0;
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var deletingDb = await Monitored<Unrelated>(db, command =>
            {
                if (command.CommandName == "delete") Interlocked.Increment(ref bulk);
                else Interlocked.Increment(ref conditional);
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(30))) throw new TimeoutException("Delete gate was not released.");
            });
            var deletion = Task.Run(() => predicate
                ? deletingDb.DeleteAsync<Unrelated>(e => true)
                : deletingDb.DeleteAsync<Unrelated>(new object[] { survivor.ID, removed.ID, removed.ID, excluded.ID }));
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await db.Collection<Unrelated>().UpdateOneAsync(e => e.ID == survivor.ID,
                                                               Builders<Unrelated>.Update.Set(e => e.Active, false));
                if (allExcluded)
                    await db.Collection<Unrelated>().UpdateOneAsync(e => e.ID == removed.ID,
                                                                   Builders<Unrelated>.Update.Set(e => e.Active, false));
            }
            finally
            {
                release.Set();
                await deletion;
            }

            var result = await deletion;
            Assert.IsTrue(result.IsAcknowledged);
            Assert.AreEqual(allExcluded ? 0L : 1L, result.DeletedCount);
            Assert.AreEqual(1, bulk);
            Assert.AreEqual(0, conditional);
            Assert.IsNotNull(await db.Find<Unrelated>().OneAsync(survivor.ID));
            Assert.IsNotNull(await db.Find<Unrelated>().OneAsync(excluded.ID));
            Assert.AreEqual(allExcluded, await db.Find<Unrelated>().OneAsync(removed.ID) != null);
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task unrelated_filtered_bulk_delete_uses_caller_transaction(bool commit)
        => Isolated(async db =>
        {
            var removed = new Unrelated();
            var excluded = new Unrelated { Active = false };
            await db.SaveAsync(new[] { removed, excluded });
            var bulk = 0;
            var conditional = 0;
            var deletingDb = await Monitored<Unrelated>(db, command =>
            {
                if (command.CommandName == "delete") Interlocked.Increment(ref bulk);
                else Interlocked.Increment(ref conditional);
                Assert.IsTrue(command.Command.Contains("txnNumber"));
                Assert.IsFalse(command.Command["autocommit"].AsBoolean);
            });
            using var transaction = deletingDb.Transaction();
            using var cancellation = new CancellationTokenSource();
            var result = await transaction.DeleteAsync<Unrelated>(new object[] { removed.ID, removed.ID, excluded.ID }, cancellation.Token);
            Assert.IsTrue(result.IsAcknowledged);
            Assert.AreEqual(1L, result.DeletedCount);
            Assert.AreEqual(1, bulk);
            Assert.AreEqual(0, conditional);
            Assert.IsNull(await transaction.Find<Unrelated>().OneAsync(removed.ID));
            Assert.IsNotNull(await db.Find<Unrelated>().OneAsync(removed.ID));
            if (commit) await transaction.CommitAsync();
            else await transaction.AbortAsync();
            Assert.AreEqual(!commit, await db.Find<Unrelated>().OneAsync(removed.ID) != null);
            Assert.IsNotNull(await db.Find<Unrelated>().OneAsync(excluded.ID));
        });

    [TestMethod]
    public Task relationship_registration_after_bulk_selection_does_not_clean_survivors_and_is_seen_next_time()
        => Isolated(async db =>
        {
            var survivor = new LateParent();
            var removed = new LateParent();
            var child = new Child();
            await db.SaveAsync(new[] { survivor, removed });
            await db.SaveAsync(child);
            var bulk = 0;
            var conditional = 0;
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var deletingDb = await Monitored<LateParent>(db, command =>
            {
                if (command.CommandName == "delete") Interlocked.Increment(ref bulk);
                else Interlocked.Increment(ref conditional);
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(30))) throw new TimeoutException("Delete gate was not released.");
            });
            var deletion = Task.Run(() => deletingDb.DeleteAsync<LateParent>(new object[] { survivor.ID, removed.ID }));
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                survivor.InitOneToMany(() => survivor.Children, db);
                await survivor.Children.AddAsync(child);
                await db.Collection<LateParent>().UpdateOneAsync(e => e.ID == survivor.ID,
                                                               Builders<LateParent>.Update.Set(e => e.Active, false));
            }
            finally
            {
                release.Set();
                await deletion;
            }

            Assert.AreEqual(1L, (await deletion).DeletedCount);
            Assert.AreEqual(1, bulk);
            Assert.AreEqual(0, conditional);
            Assert.IsNotNull(await db.Find<LateParent>().OneAsync(survivor.ID));
            Assert.AreEqual(1L, await survivor.Children.ChildrenCountAsync());

            await db.Collection<LateParent>().UpdateOneAsync(e => e.ID == survivor.ID,
                                                           Builders<LateParent>.Update.Set(e => e.Active, true));
            Assert.AreEqual(1L, (await deletingDb.DeleteAsync(survivor)).DeletedCount);
            Assert.AreEqual(1, bulk);
            Assert.AreEqual(1, conditional);
            Assert.AreEqual(0L, await survivor.Children.ChildrenCountAsync());
        });

    static async Task Isolated(Func<DB, Task> action)
    {
        var db = await InitTest.InitTestDatabase("filtered-delete-" + Guid.NewGuid().ToString("N"));
        try { await action(db); }
        finally { await db.Database().Client.DropDatabaseAsync(db.Database().DatabaseNamespace.DatabaseName); }
    }

    [TestMethod]
    [DataRow("id", false, false)]
    [DataRow("entity", false, false)]
    [DataRow("predicate", false, false)]
    [DataRow("filter", false, false)]
    [DataRow("ids", true, false)]
    [DataRow("ids", true, true)]
    [DataRow("predicate", true, true)]
    public Task concurrent_exclusion_preserves_surviving_relationships(string caller, bool mixed, bool childSide)
        => Isolated(async db =>
        {
            if (childSide) await Race<Child>(db, caller, mixed);
            else await Race<Parent>(db, caller, mixed);
        });

    [TestMethod]
    public Task concurrent_exclusion_in_transaction_reports_conflict_without_losing_relationships()
        => Isolated(db => Race<Parent>(db, "id", false, true));

    static async Task Race<T>(DB db, string caller, bool mixed, bool conflict = false) where T : Record, new()
    {
        var survivor = new T();
        var removed = new T();
        var survivorParent = survivor as Parent ?? new Parent();
        var removedParent = removed as Parent ?? new Parent();
        survivorParent.InitOneToMany(() => survivorParent.Children, db);
        removedParent.InitOneToMany(() => removedParent.Children, db);
        var survivorChild = survivor as Child ?? new Child();
        var removedChild = removed as Child ?? new Child();
        await db.SaveAsync(survivorParent);
        await db.SaveAsync(survivorChild);
        await survivorParent.Children.AddAsync(survivorChild);
        if (mixed)
        {
            await db.SaveAsync(removedParent);
            await db.SaveAsync(removedChild);
            await removedParent.Children.AddAsync(removedChild);
        }

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var gated = 0;
        var settings = db.Database().Client.Settings.Clone();
        settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
        {
            var key = command.CommandName == "delete" ? "delete" : "findAndModify";
            if (command.CommandName is not ("delete" or "findAndModify") ||
                command.Command[key] != db.CollectionName<T>() || Interlocked.Exchange(ref gated, 1) != 0)
                return;
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(30))) throw new TimeoutException("Delete gate was not released.");
        });
        var deletingDb = new FilteredDb(await DB.InitAsync(db.Database().DatabaseNamespace.DatabaseName, settings));
        using var transaction = conflict ? deletingDb.Transaction() : null;
        DB callerDb = transaction ?? (DB)deletingDb;
        var deletion = Task.Run(() => caller switch
        {
            "id" => callerDb.DeleteAsync<T>(survivor.ID),
            "entity" => callerDb.DeleteAsync(survivor),
            "predicate" => callerDb.DeleteAsync<T>(e => true),
            "filter" => callerDb.DeleteAsync<T>(f => f.Empty),
            _ => callerDb.DeleteAsync<T>(new object[] { survivor.ID, removed.ID })
        });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await db.Collection<T>().UpdateOneAsync(e => e.ID == survivor.ID, Builders<T>.Update.Set(e => e.Active, false));
        }
        finally
        {
            release.Set();
            if (conflict)
            {
                var exception = await Assert.ThrowsExactlyAsync<MongoCommandException>(() => deletion);
                Assert.IsTrue(exception.HasErrorLabel("TransientTransactionError"));
                await transaction!.AbortAsync();
            }
            else await deletion;
        }

        Assert.AreEqual(1, gated);
        if (!conflict) Assert.AreEqual(mixed ? 1L : 0L, (await deletion).DeletedCount);
        Assert.IsNotNull(await db.Find<T>().OneAsync(survivor.ID));
        Assert.AreEqual(1L, await survivorParent.Children.ChildrenCountAsync());
        if (mixed)
        {
            Assert.IsNull(await db.Find<T>().OneAsync(removed.ID));
            Assert.AreEqual(0L, await removedParent.Children.ChildrenCountAsync());
        }
        await db.Collection<T>().UpdateOneAsync(e => e.ID == survivor.ID, Builders<T>.Update.Set(e => e.Active, true));
        Assert.IsNotNull(await deletingDb.Find<T>().OneAsync(survivor.ID));
        Assert.AreEqual(1L, await survivorParent.Children.ChildrenCountAsync());
        if (typeof(T) == typeof(Parent)) Assert.IsNotNull(await db.Find<Child>().OneAsync(survivorChild.ID));
        else Assert.IsNotNull(await db.Find<Parent>().OneAsync(survivorParent.ID));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public Task filtered_delete_controls(bool excluded, bool ignore)
        => Isolated(async db =>
        {
            var parent = new Parent { Active = !excluded };
            var child = new Child();
            parent.InitOneToMany(() => parent.Children, db);
            await db.SaveAsync(parent);
            await db.SaveAsync(child);
            await parent.Children.AddAsync(child);
            var filtered = new FilteredDb(db) { IgnoreGlobalFilters = ignore };
            var count = excluded && !ignore ? 0L : 1L;
            Assert.AreEqual(count, (await filtered.DeleteAsync(parent)).DeletedCount);
            Assert.AreEqual(1L - count, await parent.Children.ChildrenCountAsync());
            Assert.AreEqual(count == 0, await db.Find<Parent>().OneAsync(parent.ID) != null);
            Assert.IsNotNull(await db.Find<Child>().OneAsync(child.ID));
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task filtered_cascade_uses_caller_transaction(bool commit)
        => Isolated(async db =>
        {
            var parent = new Parent();
            var child = new Child();
            parent.InitOneToMany(() => parent.Children, db);
            await db.SaveAsync(parent);
            await db.SaveAsync(child);
            await parent.Children.AddAsync(child);
            using var transaction = new FilteredDb(db).Transaction();
            Assert.AreEqual(1L, (await transaction.DeleteAsync(parent)).DeletedCount);
            Assert.IsNull(await transaction.Find<Parent>().OneAsync(parent.ID));
            Assert.AreEqual(0L, await parent.Children.ChildrenCountAsync(transaction.Session));
            Assert.IsNotNull(await db.Find<Parent>().OneAsync(parent.ID));
            Assert.AreEqual(1L, await parent.Children.ChildrenCountAsync());
            if (commit) await transaction.CommitAsync();
            else await transaction.AbortAsync();
            Assert.AreEqual(!commit, await db.Find<Parent>().OneAsync(parent.ID) != null);
            Assert.AreEqual(commit ? 0L : 1L, await parent.Children.ChildrenCountAsync());
            Assert.IsNotNull(await db.Find<Child>().OneAsync(child.ID));
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task cleanup_failure_is_reported_and_transaction_can_roll_back(bool transactional)
        => Isolated(async db =>
        {
            var parent = new Parent();
            var child = new Child();
            parent.InitOneToMany(() => parent.Children, db);
            await db.SaveAsync(parent);
            await db.SaveAsync(child);
            await parent.Children.AddAsync(child);
            var name = parent.Children.JoinCollection.CollectionNamespace.CollectionName;
            // A view can be read but rejects deletes, deterministically failing join cleanup.
            await db.Database().RenameCollectionAsync(name, "saved-joins");
            await db.Database().CreateViewAsync<JoinRecord, JoinRecord>(name, "saved-joins", new EmptyPipelineDefinition<JoinRecord>());
            var filtered = new FilteredDb(db);
            using var transaction = transactional ? filtered.Transaction() : null;
            DB deletingDb = transaction ?? (DB)filtered;
            await Assert.ThrowsExactlyAsync<MongoWriteException>(() => deletingDb.DeleteAsync(parent));
            if (transaction != null) await transaction.AbortAsync();
            Assert.AreEqual(transactional, await db.Find<Parent>().OneAsync(parent.ID) != null);
            Assert.AreEqual(1L, await parent.Children.ChildrenCountAsync());
            Assert.IsNotNull(await db.Find<Child>().OneAsync(child.ID));
        });

    [TestMethod]
    public Task filtered_guid_batch_preserves_stored_ids_and_counts_each_entity_once()
        => Isolated(async db =>
        {
            var parent = new GuidParent();
            var excluded = new GuidParent { Active = false };
            var child = new Child();
            parent.InitOneToMany(() => parent.Children, db);
            excluded.InitOneToMany(() => excluded.Children, db);
            await db.SaveAsync(new[] { parent, excluded });
            await db.SaveAsync(child);
            await parent.Children.AddAsync(child);
            await excluded.Children.AddAsync(child);
            var filtered = new FilteredDb(db);
            var result = await filtered.DeleteAsync<GuidParent, Guid>(new[] { parent.ID, excluded.ID, parent.ID });
            Assert.AreEqual(1L, result.DeletedCount);
            Assert.IsNull(await db.Find<GuidParent>().OneAsync(parent.ID));
            Assert.IsNotNull(await db.Find<GuidParent>().OneAsync(excluded.ID));
            Assert.AreEqual(0L, await parent.Children.ChildrenCountAsync());
            Assert.AreEqual(1L, await excluded.Children.ChildrenCountAsync());
            Assert.IsNotNull(await db.Find<Child>().OneAsync(child.ID));
        });
}
