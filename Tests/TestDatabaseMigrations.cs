using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DatabaseMigrations
{
    [TestMethod]
    public async Task supplied_database_migrations_receive_runner_and_token_and_run_once_in_order()
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            var calls = new List<int>();
            IDatabaseMigration[] migrations =
            [
                new _020_database<int>(async (runner, token) =>
                {
                    Assert.AreSame(db, runner);
                    Assert.AreEqual(cancellation.Token, token);
                    Assert.AreEqual(1L, await runner.Database().GetCollection<BsonDocument>("steps").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    calls.Add(20);
                }),
                new _010_database<int>(async (runner, token) =>
                {
                    Assert.AreSame(db, runner);
                    Assert.AreEqual(cancellation.Token, token);
                    await runner.Database().GetCollection<BsonDocument>("steps").InsertOneAsync(new BsonDocument("step", 10), cancellationToken: token);
                    calls.Add(10);
                })
            ];

            await db.MigrationsAsync(migrations, cancellation.Token);
            await db.MigrationsAsync(migrations, cancellation.Token);

            CollectionAssert.AreEqual(new[] { 10, 20 }, calls);
            CollectionAssert.AreEqual(new[] { 10, 20 }, await History(db));
        });
    }

    [TestMethod]
    public async Task legacy_supplied_migrations_and_original_method_groups_still_work()
    {
        await WithDatabase(async db =>
        {
            var calls = 0;
            Func<Task> discover = db.MigrateAsync;
            Func<Task> discoverAssembly = db.MigrateAsync<Migrations>;
            Func<IEnumerable<IMigration>, Task> execute = db.MigrationsAsync;
            Action<Func<Type, IMigration>> setActivator = db.SetMigrationActivator;
            Assert.IsNotNull(discover);
            Assert.IsNotNull(discoverAssembly);
            Assert.IsNotNull(setActivator);

            await execute(new IMigration[] { new _005_legacy<int>(() => { calls++; return Task.CompletedTask; }) });
            Assert.AreEqual(1, calls);
            CollectionAssert.AreEqual(new[] { 5 }, await History(db));
        });
    }

    [TestMethod]
    [DataRow("discover")]
    [DataRow("assembly")]
    [DataRow("legacy")]
    [DataRow("database")]
    public async Task cancellation_before_start_runs_nothing(string entryPoint)
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var calls = 0;
            db.SetMigrationActivator((Func<Type, object>)(_ => { calls++; throw new AssertFailedException("Should not construct migrations"); }));

            await Assert.ThrowsAsync<OperationCanceledException>(() => entryPoint switch
            {
                "discover" => db.MigrateAsync(cancellation.Token),
                "assembly" => db.MigrateAsync<Migrations>(cancellation.Token),
                "legacy" => db.MigrationsAsync(new IMigration[] { new _005_legacy<int>(() => { calls++; return Task.CompletedTask; }) }, cancellation.Token),
                _ => db.MigrationsAsync(new IDatabaseMigration[] { new _010_database<int>((_, _) => { calls++; return Task.CompletedTask; }) }, cancellation.Token)
            });

            Assert.AreEqual(0, calls);
            CollectionAssert.AreEqual(Array.Empty<int>(), await History(db));
        });
    }

    [TestMethod]
    public async Task cancellation_interrupts_cooperative_body_without_recording_completion()
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var laterCalled = false;
            var run = db.MigrationsAsync(new IDatabaseMigration[]
            {
                new _010_database<int>(async (_, token) =>
                {
                    Assert.AreEqual(cancellation.Token, token);
                    entered.SetResult();
                    await Task.Delay(Timeout.Infinite, token);
                }),
                new _020_database<int>((_, _) => { laterCalled = true; return Task.CompletedTask; })
            }, cancellation.Token);

            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally
            {
                cancellation.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => run);
            }

            Assert.IsFalse(laterCalled);
            CollectionAssert.AreEqual(Array.Empty<int>(), await History(db));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task cancellation_after_body_returns_does_not_record_history(bool legacy)
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            var calls = 0;
            Task Cancel()
            {
                calls++;
                cancellation.Cancel();
                return Task.CompletedTask;
            }

            await Assert.ThrowsAsync<OperationCanceledException>(() => legacy
                ? db.MigrationsAsync(new IMigration[] { new _005_legacy<int>(Cancel) }, cancellation.Token)
                : db.MigrationsAsync(new IDatabaseMigration[] { new _010_database<int>((_, _) => Cancel()) }, cancellation.Token));

            Assert.AreEqual(1, calls);
            CollectionAssert.AreEqual(Array.Empty<int>(), await History(db));
        });
    }

    [TestMethod]
    public async Task failed_migration_is_not_recorded_and_later_migrations_do_not_run()
    {
        await WithDatabase(async db =>
        {
            var failure = new InvalidOperationException("Migration failed");
            var laterCalled = false;
            var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => db.MigrationsAsync(new IDatabaseMigration[]
            {
                new _010_database<int>((_, _) => Task.FromException(failure)),
                new _020_database<int>((_, _) => { laterCalled = true; return Task.CompletedTask; })
            }));

            Assert.AreSame(failure, error);
            Assert.IsFalse(laterCalled);
            CollectionAssert.AreEqual(Array.Empty<int>(), await History(db));
        });
    }

    [TestMethod]
    public async Task retry_after_cancellation_skips_completed_steps_but_retries_unrecorded_changes()
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            var firstCalls = 0;
            var secondCalls = 0;
            IDatabaseMigration[] migrations =
            [
                new _010_database<int>((_, _) => { firstCalls++; return Task.CompletedTask; }),
                new _020_database<int>(async (runner, token) =>
                {
                    await runner.Database().GetCollection<BsonDocument>("steps").InsertOneAsync(new BsonDocument("attempt", ++secondCalls), cancellationToken: token);
                    if (secondCalls == 1)
                        cancellation.Cancel();
                })
            ];

            await Assert.ThrowsAsync<OperationCanceledException>(() => db.MigrationsAsync(migrations, cancellation.Token));
            CollectionAssert.AreEqual(new[] { 10 }, await History(db));
            Assert.AreEqual(1L, await db.Database().GetCollection<BsonDocument>("steps").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));

            await db.MigrationsAsync(migrations);
            Assert.AreEqual(1, firstCalls);
            Assert.AreEqual(2, secondCalls);
            CollectionAssert.AreEqual(new[] { 10, 20 }, await History(db));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task dual_contract_migration_runs_database_body_only(bool legacyCollection)
    {
        await WithDatabase(async db =>
        {
            var migration = new _030_dual<int>();
            if (legacyCollection)
                await db.MigrationsAsync(new IMigration[] { migration });
            else
                await db.MigrationsAsync(new IDatabaseMigration[] { migration });

            Assert.AreEqual(1, migration.DatabaseCalls);
            Assert.AreSame(db, migration.Database);
            CollectionAssert.AreEqual(new[] { 30 }, await History(db));
        });
    }

    [TestMethod]
    [DataRow("default")]
    [DataRow("activator")]
    [DataRow("provider")]
    public async Task discovery_supports_both_contracts_and_dependency_construction(string construction)
    {
        await WithDatabase(async db =>
        {
            var constructed = new List<Type>();
            var provider = new MigrationProvider(constructed);
            if (construction == "activator")
            {
                db.SetServiceProvider(new RejectingProvider());
                db.SetMigrationActivator(type => provider.GetService(type)!);
            }
            else if (construction == "provider")
                db.SetServiceProvider(provider);

            var books = db.Database().GetCollection<BsonDocument>(db.Collection<BookEntity>().CollectionNamespace.CollectionName);
            await books.InsertOneAsync(new BsonDocument { { "_id", "migration-book" }, { "Price", 42 } });
            using var cancellation = new CancellationTokenSource();
            await db.MigrateAsync<Migrations>(cancellation.Token);
            CollectionAssert.AreEqual(new[] { 1, 2 }, await History(db));
            var book = await books.Find(new BsonDocument("_id", "migration-book")).SingleAsync();
            Assert.AreEqual(42, book["SellingPrice"].AsInt32);
            Assert.IsFalse(book.Contains("Price"));
            if (construction != "default")
                CollectionAssert.AreEquivalent(new[] { typeof(_001_rename_field), typeof(_002_undo_field_rename) }, constructed);
        });
    }

    [TestMethod]
    public async Task cancellation_reaches_history_read()
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            var settings = db.Database().Client.Settings.Clone();
            var observed = false;
            settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.DatabaseNamespace.DatabaseName == db.DatabaseName() && command.CommandName == "find" &&
                    command.Command["find"] == "_migration_history_")
                {
                    observed = true;
                    cancellation.Cancel();
                }
            });
            var runner = await DB.InitAsync(db.DatabaseName(), settings);
            var bodyCalled = false;

            await Assert.ThrowsAsync<OperationCanceledException>(() => runner.MigrationsAsync(new IDatabaseMigration[]
            {
                new _010_database<int>((_, _) => { bodyCalled = true; return Task.CompletedTask; })
            }, cancellation.Token));

            Assert.IsTrue(observed);
            Assert.IsFalse(bodyCalled);
            CollectionAssert.AreEqual(Array.Empty<int>(), await History(db));
        });
    }

    [TestMethod]
    public async Task cancellation_reaches_history_write()
    {
        await WithDatabase(async db =>
        {
            using var cancellation = new CancellationTokenSource();
            var runner = new CancelHistoryDb(db, cancellation);
            var bodyCalled = false;
            await Assert.ThrowsAsync<OperationCanceledException>(() => runner.MigrationsAsync(new IDatabaseMigration[]
            {
                new _010_database<int>((_, _) => { bodyCalled = true; return Task.CompletedTask; })
            }, cancellation.Token));

            Assert.IsTrue(bodyCalled);
            Assert.IsTrue(runner.HistorySaveCalled);
            CollectionAssert.AreEqual(Array.Empty<int>(), await History(db));
        });
    }

    static async Task<int[]> History(DB db)
        => (await db.Find<Migration>().Sort(m => m.Number, Order.Ascending).ExecuteAsync()).Select(m => m.Number).ToArray();

    static async Task WithDatabase(Func<DB, Task> test)
    {
        var db = await InitTest.InitTestDatabase("database-migrations-" + Guid.NewGuid().ToString("N"));
        try
        {
            await test(db);
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.DatabaseName());
        }
    }

    // Open generic fixtures are not discoverable migrations. Tests supply closed instances explicitly.
    public class _010_database<T>(Func<DB, CancellationToken, Task> upgrade) : IDatabaseMigration
    {
        public Task UpgradeAsync(DB db, CancellationToken cancellation = default) => upgrade(db, cancellation);
    }

    public class _020_database<T>(Func<DB, CancellationToken, Task> upgrade) : _010_database<T>(upgrade);

    public class _005_legacy<T>(Func<Task> upgrade) : IMigration
    {
        public Task UpgradeAsync() => upgrade();
    }

    public class _030_dual<T> : IMigration, IDatabaseMigration
    {
        public int DatabaseCalls { get; private set; }
        public DB? Database { get; private set; }

        public Task UpgradeAsync() => throw new AssertFailedException("Legacy body must not run for dual-contract migrations");

        public Task UpgradeAsync(DB db, CancellationToken cancellation = default)
        {
            DatabaseCalls++;
            Database = db;
            return Task.CompletedTask;
        }
    }

    class MigrationProvider(List<Type> constructed) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            constructed.Add(serviceType);
            return Activator.CreateInstance(serviceType);
        }
    }

    class RejectingProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => throw new AssertFailedException("Activator must take precedence");
    }

    class CancelHistoryDb(DB source, CancellationTokenSource cancellation) : DB(source)
    {
        public bool HistorySaveCalled { get; private set; }

        protected override Action<T>? OnBeforeSave<T>()
            => _ =>
            {
                if (typeof(T) != typeof(Migration))
                    return;

                HistorySaveCalled = true;
                cancellation.Cancel();
            };
    }
}
