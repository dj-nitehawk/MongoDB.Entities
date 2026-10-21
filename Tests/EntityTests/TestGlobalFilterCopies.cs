using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class GlobalFilterCopyTests
{
    [TestMethod]
    [DataRow("plain")]
    [DataRow("audit")]
    [DataRow("transaction")]
    public async Task copies_keep_source_audit_and_pending_queries_isolated(string kind)
    {
        await WithDatabase(async raw =>
        {
            var source = new TenantDb(raw, "A");
            var audit = source.WithModifiedBy(new ModifiedBy());
            var pending = source.Find<FilterRecord>();
            using var transaction = kind == "transaction" ? source.Transaction() : null;
            var other = new TenantDb((DB?)transaction ?? (kind == "audit" ? audit : source), "B");

            Assert.AreEqual("A", (await source.Find<FilterRecord>().ExecuteAsync()).Single().Tenant);
            Assert.AreEqual("A", (await audit.Find<FilterRecord>().ExecuteAsync()).Single().Tenant);
            Assert.AreEqual("A", (await pending.ExecuteAsync()).Single().Tenant);
            Assert.AreEqual(1L, await source.CountAsync<FilterRecord>());
            Assert.AreEqual(2L, await other.CountAsync<FilterRecord>());
            Assert.IsTrue((await other.Find<FilterRecord>().ExecuteAsync()).All(e => e.Tenant == "B"));
            if (transaction is not null)
            {
                Assert.AreEqual(1L, await transaction.CountAsync<FilterRecord>());
                await transaction.AbortAsync();
            }

            Assert.AreEqual(1L, (await source.DeleteAsync<FilterRecord>(e => true)).DeletedCount);
            Assert.AreEqual(2L, await raw.CountAsync<FilterRecord>());
            Assert.IsTrue((await raw.Find<FilterRecord>().ExecuteAsync()).All(e => e.Tenant == "B"));
        });
    }

    [TestMethod]
    public async Task registrations_are_independent_and_existing_builders_observe_same_instance_changes()
    {
        await WithDatabase(async raw =>
        {
            await raw.SaveAsync(new[] { new ExtraRecord { Tenant = "A" }, new ExtraRecord { Tenant = "B" } });
            var source = new TenantDb(raw, "A");
            var copy = new TenantDb(source);
            Assert.AreEqual(1L, await copy.CountAsync<FilterRecord>());
            copy.FilterExtras("B");
            Assert.AreEqual(2L, await source.CountAsync<ExtraRecord>());
            Assert.AreEqual(1L, await copy.CountAsync<ExtraRecord>());
            var pending = source.Find<FilterRecord>();
            source.FilterTenant("B");
            Assert.AreEqual(2, (await pending.ExecuteAsync()).Count);
            Assert.AreEqual(1L, await copy.CountAsync<FilterRecord>());
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task audit_and_transaction_hooks_use_the_owning_views_filter_snapshot(bool commit)
    {
        await WithDatabase(async raw =>
        {
            var source = new TenantDb(raw, "A");
            var audit = source.WithModifiedBy(new ModifiedBy { UserID = "audit" });
            using (var transaction = audit.Transaction())
            {
                var view = transaction.WithModifiedBy(new ModifiedBy { UserID = "transaction" });
                source.FilterTenant("B");
                var other = new TenantDb(view, "B");
                Assert.AreEqual(2L, await other.CountAsync<FilterRecord>());
                var result = await view.Update<FilterRecord>().Match(e => true).Modify(e => e.Value, 10).ExecuteAsync();
                Assert.AreEqual(1L, result.ModifiedCount);
                var saved = (await view.Find<FilterRecord>().ExecuteAsync()).Single();
                Assert.AreEqual("A", saved.Tenant);
                Assert.AreEqual(1L, saved.HookCount);
                Assert.AreEqual("transaction", saved.HookUser);
                Assert.AreEqual(0, (await raw.Find<FilterRecord>().OneAsync(saved.ID))?.Value);
                if (commit)
                    await transaction.CommitAsync();
                else
                    await transaction.AbortAsync();
            }
            var a = (await audit.Find<FilterRecord>().ExecuteAsync()).Single();
            Assert.AreEqual(commit ? 10 : 0, a.Value);
            Assert.IsTrue((await source.Find<FilterRecord>().ExecuteAsync()).All(e => e.Value == 0));
            await audit.Update<FilterRecord>().Match(e => true).Modify(e => e.Value, 20).ExecuteAsync();
            a = (await audit.Find<FilterRecord>().ExecuteAsync()).Single();
            Assert.AreEqual(1L, a.HookCount);
            Assert.AreEqual("audit", a.HookUser);
        });
    }

    [TestMethod]
    [DataRow("id", "none")]
    [DataRow("predicate", "none")]
    [DataRow("id", "commit")]
    [DataRow("predicate", "commit")]
    [DataRow("id", "abort")]
    [DataRow("predicate", "abort")]
    public async Task copied_filters_preserve_other_tenants_and_relationships_on_delete(string mode, string outcome)
    {
        await WithDatabase(async raw =>
        {
            var records = await raw.Find<FilterRecord>().ExecuteAsync();
            var a = records.Single(e => e.Tenant == "A");
            var b = records.First(e => e.Tenant == "B");
            // Relationship helpers must be bound to this disposable database.
            a.InitOneToMany(() => a.Children, raw);
            b.InitOneToMany(() => b.Children, raw);
            await a.Children.AddAsync(b);
            await b.Children.AddAsync(a);
            var source = new TenantDb(raw, "A");
            using (var transaction = outcome == "none" ? null : source.Transaction())
            {
                var view = (DB?)transaction ?? source;
                _ = new TenantDb(source, "B");
                var result = mode == "id"
                                 ? await view.DeleteAsync<FilterRecord>(new[] { a.ID, b.ID })
                                 : await view.DeleteAsync<FilterRecord>(e => true);
                Assert.AreEqual(1L, result.DeletedCount);
                Assert.AreEqual(0L, await view.CountAsync<FilterRecord>());
                if (transaction is not null)
                {
                    if (outcome == "commit")
                        await transaction.CommitAsync();
                    else
                        await transaction.AbortAsync();
                }
            }
            Assert.AreEqual(outcome == "abort" ? 3L : 2L, await raw.CountAsync<FilterRecord>());
            Assert.AreEqual(2, (await raw.Find<FilterRecord>().Match(e => e.Tenant == "B").ExecuteAsync()).Count);
            Assert.AreEqual(outcome == "abort" ? 1L : 0L, await a.Children.ChildrenCountAsync());
            Assert.AreEqual(outcome == "abort" ? 1L : 0L, await b.Children.ChildrenCountAsync());
        });
    }

    static async Task WithDatabase(Func<DB, Task> test)
    {
        var db = await InitTest.InitTestDatabase("filter-copy-" + Guid.NewGuid().ToString("N"));
        try
        {
            await db.SaveAsync(new[]
            {
                new FilterRecord { Tenant = "A" },
                new FilterRecord { Tenant = "B" },
                new FilterRecord { Tenant = "B" }
            });
            await test(db);
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.DatabaseName());
        }
    }

    public class FilterRecord : Entity
    {
        public string Tenant { get; set; } = "";
        public int Value { get; set; }
        public long HookCount { get; set; }
        public string HookUser { get; set; } = "";
        public Many<FilterRecord, FilterRecord> Children { get; set; }

        public FilterRecord() => this.InitOneToMany(() => Children);
    }

    public class ExtraRecord : Entity
    {
        public string Tenant { get; set; } = "";
    }

    public class TenantDb : DB
    {
        public TenantDb(DB source, string? tenant = null) : base(source)
        {
            if (tenant is not null)
                FilterTenant(tenant);
        }

        public void FilterTenant(string tenant) => SetGlobalFilter<FilterRecord>(e => e.Tenant == tenant);
        public void FilterExtras(string tenant) => SetGlobalFilter<ExtraRecord>(e => e.Tenant == tenant);

        protected override Action<UpdateBase<T>>? OnBeforeUpdate<T>()
            => update =>
            {
                if (update is UpdateBase<FilterRecord> record)
                {
                    record.AddModification(e => e.HookCount, CountAsync<FilterRecord>().GetAwaiter().GetResult());
                    record.AddModification(e => e.HookUser, ModifiedBy?.UserID ?? "");
                }
            };
    }
}
