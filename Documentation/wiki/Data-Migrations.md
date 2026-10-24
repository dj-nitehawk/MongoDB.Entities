# Migration system

Write migration classes to transform stored documents when your application's schema changes. Normally, implement `IMigration.UpgradeAsync()` and supply any dependencies through the constructor. Give each class a name starting with a numeric prefix, such as `_001_merge_author_names` or `_002_rename_author_name`.

Alternatively, implement `IDatabaseMigration` to receive the runner's database and cancellation token directly in `UpgradeAsync`. Both approaches are valid and supported; choose the one that fits your migration.

## Run migrations

The library records completed migrations in `_migration_history_` and runs migrations whose numbers are greater than the last completed number, in numeric order. Run them after database initialization, before accepting application traffic:

```csharp
await db.MigrateAsync();
```

This discovers concrete, closed classes implementing either interface in loaded application assemblies, excluding framework and other known assembly prefixes. Both contracts share numeric ordering and the same history collection. Restrict discovery to a particular assembly by supplying one of its types:

```csharp
await db.MigrateAsync<SomeType>();
```

By default, discovered classes need parameterless constructors. To supply dependencies, either configure `db.SetMigrationActivator(...)` or `db.SetServiceProvider(...)`, or pass constructed instances to `db.MigrationsAsync(...)`. The activator takes precedence over the service provider.

Use the activator overload accepting `Func<Type, IMigration>` for `IMigration` classes, or `Func<Type, object>` for either interface, including applications using both. Return an instance implementing one of the migration interfaces. With a service provider, register each concrete migration class.

The `IMigration` examples below accept a `DB` in their constructors. Run them explicitly so they target the same database that records their completion:

```csharp
await db.MigrationsAsync(new IMigration[]
{
    new _001_merge_author_names(db),
    new _002_rename_author_name(db),
    new _003_rename_author_collection(db)
});
```

Migration execution is not automatically transactional. Back up data, test on a restored copy, and coordinate startup so multiple application instances do not run migrations concurrently. Make migration steps safe to retry if a process stops before completion is recorded.

## Receive the database directly

Use `IDatabaseMigration` if you prefer the runner to supply the database and cancellation token as method parameters. The runner supplies the same `DB` instance that reads and writes migration history:

```csharp
using MongoDB.Entities;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

public class _001_rename_collection : IDatabaseMigration
{
    public Task UpgradeAsync(DB db, CancellationToken cancellation = default)
        => db.Database().RenameCollectionAsync("OldName", "NewName", cancellationToken: cancellation);
}
```

No activator or DI configuration is needed for parameterless classes like this one. Constructor injection is still available for other dependencies.

The interfaces are independent; implement only the one you need. If a class implements both, the runner calls only its `IDatabaseMigration` method.

Pass constructed database migrations with an `IDatabaseMigration` collection:

```csharp
await db.MigrationsAsync(new IDatabaseMigration[]
{
    new _001_rename_collection()
});
```

The supplied-instance overloads accept `IEnumerable<IMigration>` or `IEnumerable<IDatabaseMigration>`. Use discovery to run a mixture of the two contracts together in numeric order. Because the overloads accept different interfaces, cast a dual-contract collection to the intended interface explicitly; a bare `null` is also ambiguous.

## Cancellation and retries

Pass a cancellation token to any migration entry point:

```csharp
using var cancellation = new CancellationTokenSource();
await db.MigrateAsync(cancellation.Token);
// Or restrict discovery:
await db.MigrateAsync<SomeType>(cancellation.Token);
// Or supply instances:
await db.MigrationsAsync(new IDatabaseMigration[] { new _001_rename_collection() }, cancellation.Token);
```

The runner uses the token for history reads and writes, checks it before activation and around migration bodies, and passes it to `IDatabaseMigration.UpgradeAsync`. Forward it to your own I/O operations. `IMigration` bodies do not receive the token and cannot be interrupted by the runner; cancellation is observed after they return.

Cancellation is cooperative and does not roll back changes. If a migration fails or cancellation is observed before its completion is recorded, later migrations do not run. Its changes may already be persisted, and the migration may run again on retry. A canceled or failed history write can also have an uncertain outcome. Previously recorded migrations remain completed. Make migration steps safe to retry and reconcile uncertain outcomes before restarting.

## Examples

These examples assume an existing `Author` collection with the following model:

```csharp
public class Author : Entity
{
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string FullName { get; set; } = "";
}
```

Include `using MongoDB.Entities;`, `using MongoDB.Driver;`, and `using System.Threading.Tasks;` for the examples below.

### Merge two properties

This version reads each author's first and last name and updates only that author's document:

```csharp
public class _001_merge_author_names(DB db) : IMigration
{
    public async Task UpgradeAsync()
    {
        await db.Fluent<Author>()
                .Project(a => new { id = a.ID, fullname = a.Name + " " + a.Surname })
                .ForEachAsync(async a =>
                {
                    await db.Update<Author>()
                            .MatchID(a.id)
                            .Modify(x => x.FullName, a.fullname)
                            .ExecuteAsync();
                });
    }
}
```

For large collections, use the following alternative to compute the value on the server with a single update pipeline (MongoDB 4.2 or newer). Choose one implementation of `_001_merge_author_names`, not both:

```csharp
public class _001_merge_author_names(DB db) : IMigration
{
    public Task UpgradeAsync()
    {
        return db.Update<Author>()
                 .Match(_ => true)
                 .WithPipelineStage("{$set:{FullName:{$concat:['$Name',' ','$Surname']}}}")
                 .ExecutePipelineAsync();
    }
}
```

### Rename a property

```csharp
public class _002_rename_author_name(DB db) : IMigration
{
    public Task UpgradeAsync()
    {
        return db.Update<Author>()
                 .Match(_ => true)
                 .Modify(b => b.Rename("FullName", "AuthorName"))
                 .ExecuteAsync();
    }
}
```

### Rename a collection

```csharp
public class _003_rename_author_collection(DB db) : IMigration
{
    public Task UpgradeAsync()
    {
        return db.Database().RenameCollectionAsync("Author", "Writer");
    }
}
```

After renaming, map your application model to `Writer` with `[Collection("Writer")]`, or rename the entity type accordingly. The simple rename example is not safe to rerun after the collection has already been renamed; check source/destination collection state before retrying it.
