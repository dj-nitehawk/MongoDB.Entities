# Migration system

Write migration classes to transform stored documents when your application's schema changes. Each class implements `IMigration.UpgradeAsync()` and has a name starting with a numeric prefix, such as `_001_merge_author_names` or `_002_rename_author_name`.

## Run migrations

The library records completed migrations in `_migration_history_` and runs migrations whose numbers are greater than the last completed number, in numeric order. Run them after database initialization, before accepting application traffic:

```csharp
await db.MigrateAsync();
```

This discovers migrations in loaded application assemblies, excluding framework and other known assembly prefixes. Restrict discovery to a particular assembly by supplying one of its types:

```csharp
await db.MigrateAsync<SomeType>();
```

By default, discovered classes need parameterless constructors. To supply dependencies, either configure `db.SetMigrationActivator(...)` or `db.SetServiceProvider(...)`, or pass constructed instances to `db.MigrationsAsync(...)`. The activator takes precedence over the service provider.

The examples below accept a `DB` in their constructors. Run them explicitly so they target the same database that records their completion:

```csharp
await db.MigrationsAsync(new IMigration[]
{
    new _001_merge_author_names(db),
    new _002_rename_author_name(db),
    new _003_rename_author_collection(db)
});
```

Migration execution is not automatically transactional. Back up data, test on a restored copy, and coordinate startup so multiple application instances do not run migrations concurrently. Make migration steps safe to retry if a process stops before completion is recorded.

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
