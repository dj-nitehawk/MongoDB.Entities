using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class PipelineHookTests
{
    const string Stage = "{ $set: { Name: 'pipeline' } }";
    const string MixedUpdates = "Pipeline updates cannot be used with modifications added by OnBeforeUpdate!";

    [TestMethod]
    [DataRow("update", false)]
    [DataRow("get", false)]
    [DataRow("project", false)]
    [DataRow("update", true)]
    [DataRow("get", true)]
    [DataRow("project", true)]
    public async Task pipeline_hooks_run_once_and_never_discard_modifications(string builder, bool auditView)
    {
        await WithDatabase(async db =>
        {
            foreach (var mode in new[] { "reject", "modify", "same-field", "observe" })
            {
                var entity = new PipelineHookEntity();
                await db.SaveAsync(entity);
                var source = new HookDb(db.WithModifiedBy(User("source")), mode);
                var view = auditView ? source.WithModifiedBy(User("view")) : source;
                if (mode == "observe")
                {
                    var result = await Execute(view, entity.ID, builder);
                    if (builder != "update")
                        Assert.AreEqual("pipeline", result);
                }
                else if (mode == "reject")
                {
                    var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Execute(view, entity.ID, builder));
                    Assert.AreEqual("Rejected by update hook", error.Message);
                }
                else
                {
                    var error = await Assert.ThrowsExactlyAsync<ArgumentException>(() => Execute(view, entity.ID, builder));
                    Assert.AreEqual(MixedUpdates, error.Message);
                }

                Assert.HasCount(1, source.Calls);
                var user = auditView ? "view" : "source";
                Assert.AreEqual($"{user}/{user}/1", source.Calls[0]);
                Assert.AreEqual("source", source.ModifiedBy?.UserID);
                var saved = await db.Find<PipelineHookEntity>().OneAsync(entity.ID);
                Assert.IsNotNull(saved);
                Assert.AreEqual(mode == "observe" ? "pipeline" : "original", saved.Name);
                Assert.AreEqual("original", saved.HookValue);
                await db.DeleteAsync(entity);
            }
        });
    }

    [TestMethod]
    [DataRow("update")]
    [DataRow("get")]
    [DataRow("project")]
    public async Task pipeline_validation_hooks_preserve_global_filters(string builder)
    {
        await WithDatabase(async db =>
        {
            var hidden = new PipelineHookEntity { Visible = false };
            var visible = new PipelineHookEntity();
            await db.SaveAsync(new[] { hidden, visible });
            var source = new HookDb(db, "observe");
            var view = source.WithModifiedBy(User("view"));
            await Execute(view, hidden.ID, builder);
            await Execute(view, visible.ID, builder);
            Assert.HasCount(2, source.Calls);
            Assert.AreEqual("original", (await db.Find<PipelineHookEntity>().OneAsync(hidden.ID))?.Name);
            Assert.AreEqual("pipeline", (await db.Find<PipelineHookEntity>().OneAsync(visible.ID))?.Name);
        });
    }

    [TestMethod]
    [DataRow("update", true)]
    [DataRow("get", true)]
    [DataRow("project", true)]
    [DataRow("update", false)]
    [DataRow("get", false)]
    [DataRow("project", false)]
    public async Task pipeline_hooks_preserve_transaction_context(string builder, bool commit)
    {
        foreach (var mode in new[] { "reject", "modify", "same-field", "observe" })
        {
            await WithDatabase(async db =>
            {
                var entity = new PipelineHookEntity();
                await db.SaveAsync(entity);
                var source = new HookDb(db, mode);
                var expected = mode == "observe" ? "pipeline" : "original";
                using (var transaction = source.WithModifiedBy(User("outer")).Transaction())
                {
                    var view = transaction.WithModifiedBy(User("transaction"));
                    await view.SaveAsync(new PipelineHookEntity());
                    using var cancellation = new CancellationTokenSource();
                    if (mode == "reject")
                        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Execute(view, entity.ID, builder, cancellation: cancellation.Token));
                    else if (mode != "observe")
                        await Assert.ThrowsExactlyAsync<ArgumentException>(() => Execute(view, entity.ID, builder, cancellation: cancellation.Token));
                    else
                        await Execute(view, entity.ID, builder, cancellation: cancellation.Token);
                    Assert.HasCount(1, source.Calls);
                    Assert.AreEqual("transaction/transaction/2", source.Calls[0]);
                    Assert.AreEqual(expected, (await view.Find<PipelineHookEntity>().OneAsync(entity.ID))?.Name);
                    Assert.AreEqual("original", (await db.Find<PipelineHookEntity>().OneAsync(entity.ID))?.Name);
                    if (commit)
                        await transaction.CommitAsync();
                    else
                        await transaction.AbortAsync();
                }
                Assert.AreEqual(commit ? expected : "original", (await db.Find<PipelineHookEntity>().OneAsync(entity.ID))?.Name);
            });
        }
    }

    static async Task<string?> Execute(DB db, string id, string builder, CancellationToken cancellation = default)
    {
        if (builder == "update")
        {
            var update = db.Update<PipelineHookEntity>().MatchID(id).WithPipelineStage(Stage);
            await update.ExecutePipelineAsync(cancellation);
            return null;
        }
        if (builder == "get")
        {
            var update = db.UpdateAndGet<PipelineHookEntity>().MatchID(id).WithPipelineStage(Stage);
            return (await update.ExecutePipelineAsync(cancellation))?.Name;
        }
        var projected = db.UpdateAndGet<PipelineHookEntity>().MatchID(id).WithPipelineStage(Stage)
                          .Project(e => new PipelineHookEntity { Name = e.Name });
        return (await projected.ExecutePipelineAsync(cancellation))?.Name;
    }

    static ModifiedBy User(string id) => new() { UserID = id };

    static async Task WithDatabase(Func<DB, Task> test)
    {
        var db = await InitTest.InitTestDatabase("pipeline-hooks-" + Guid.NewGuid().ToString("N"));
        try
        {
            await db.Database().CreateCollectionAsync(nameof(PipelineHookEntity));
            await test(db.WithModifiedBy(User("seed")));
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.DatabaseName());
        }
    }

    public class PipelineHookEntity : Entity
    {
        public string Name { get; set; } = "original";
        public string HookValue { get; set; } = "original";
        public bool Visible { get; set; } = true;
        public ModifiedBy ModifiedBy { get; set; } = new();
    }

    class HookDb : DB
    {
        readonly string _mode;
        public List<string> Calls { get; } = [];

        public HookDb(DB source, string mode) : base(source)
        {
            _mode = mode;
            SetGlobalFilter<PipelineHookEntity>(e => e.Visible);
        }

        protected override Action<UpdateBase<T>>? OnBeforeUpdate<T>()
        {
            var factoryUser = ModifiedBy?.UserID;
            return update =>
            {
                Calls.Add($"{factoryUser}/{ModifiedBy?.UserID}/{CountAsync<PipelineHookEntity>().GetAwaiter().GetResult()}");
                if (_mode == "reject")
                    throw new InvalidOperationException("Rejected by update hook");
                if (update is UpdateBase<PipelineHookEntity> pipeline)
                {
                    if (_mode == "modify")
                        pipeline.AddModification(e => e.HookValue, "$Name");
                    else if (_mode == "same-field")
                        pipeline.AddModification(e => e.Name, "hook");
                }
            };
        }
    }
}
