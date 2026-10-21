# Global filters

Global filters let you define common criteria once for a `DB` instance. The library applies them to its filter-aware retrieval, update, count, and delete APIs. They are not a database-level access-control boundary: saves, raw collection access, and some relationship operations bypass them. See the limitations below.

To be able to specify common criteria, you need to create a derived `DB` class so:

```csharp
public class MyDatabase : DB
{
    public MyDatabase(string dbName) : base(Instance(dbName))
    {
        SetGlobalFilter<Book>(
            b => b.Publisher == "Harper Collins" &&
                 b.IsDeleted == false);

        SetGlobalFilter<Author>(
            a => a.Status == "Active" &&
                 a.IsDeleted == false);
    }
}
```

You can then instantiate your derived `DB` instance from anywhere like so:

```csharp
var db = new MyDatabase("DatabaseName");
```

## Copying database instances

Constructing a derived database with `base(source)`, creating an audit view with `WithModifiedBy()`, or starting a transaction copies the source's current filter registrations. The copy inherits all registered entity filters, including their prepend settings. Registering or replacing a filter on either instance later does not change the other instance's filters.

Previously created query builders on the source are unaffected by registrations on a copy. Builders that retain an existing registration dictionary still observe intentional filter replacements on their own instance. Configure filters before creating builders; a builder created before the instance's first registration does not acquire that filter later.

Derived hooks retained by audit views and transactions use the owning view's filters, audit data, and session. Filter copying does not create or dispose a session: audit views share the transaction session, while a new transaction owns its own session.

## Specify filters using a base class

Filters can be specified on a per-entity type basis like above or common filters can be specified using a base class type like so:

```csharp
SetGlobalFilterForBaseClass<BaseEntity>(x => x.IsDeleted == false);
```

## Specify filters using an interface

If you'd like a global filter to be applied to any entity type that implements an interface, you can specify it like below using a json string.
It is currently not possible to do it in a strongly typed manner due to a limitation in the driver.

```csharp
SetGlobalFilterForInterface<ISoftDeletable>("{ IsDeleted : false }");
```

## Prepending global filters

Global filters by default are appended to your operation filters. If you'd like to instead have the global filters prepended, use the following overload:

```csharp
SetGlobalFilter<Book>(
    filter: b => b.Publisher == "Harper Collins",
    prepend: true);
```

## Temporarily ignoring global filters

It's possible to skip/ignore global filters on a per operation basis as follows:

```csharp
//with fluent operations
await db.Find<Book>()
        .Match(b => b.Title == "Power Of Tomorrow")
        .IgnoreGlobalFilters() //ignored only for this operation
        .ExecuteAsync();

//with direct methods:
var previouslyIgnored = db.IgnoreGlobalFilters;
try
{
    db.IgnoreGlobalFilters = true;
    await db.DeleteAsync<Book>(b => b.Title == "Power Of Tomorrow");
}
finally
{
    db.IgnoreGlobalFilters = previouslyIgnored;
}
```

The instance flag affects APIs that honor it. It changes shared state on that instance, so do not toggle it while other operations use the same instance concurrently.

`CountAsync` uses its own per-call argument rather than the instance flag:

```csharp
var count = await db.CountAsync<Book>(_ => true, ignoreGlobalFilters: true);
```

## Limitations

1. Only one filter per entity type is allowed. Specify multiple criteria for the same entity type with the `&&` operator as shown above. If you call `SetGlobalFilter<Book>` more than once, only the last call will be registered.

2. If using a base class to specify filters, no derived entity type (of that specific base class) can be used for registering another filter. Take the following for example:

```csharp
    SetGlobalFilter<Book>(b => b.Publisher == "Harper Collins");

    SetGlobalFilterForBaseClass<BaseEntity>(x => x.IsDeleted == false);
```

Only the second filter would take effect. The first one is discarded because the `Book` type is a derived type of `BaseEntity`.

You can however switch the order of registration so that the base class registration occurs first. But you need to make sure to include the criteria the base class registration caters to as well, like so:

```csharp
    SetGlobalFilterForBaseClass<BaseEntity>(x => x.IsDeleted == false);

    SetGlobalFilter<Book>(
          b => b.Publisher == "Harper Collins" &&
               b.IsDeleted == false);
``` 

3. `Save*()` operations ignore global filters and match on the ID field. Inserts, raw `Collection<T>()` access, and `CountEstimatedAsync` also bypass global filters. Relationship APIs do not uniformly apply entity filters, so do not rely on them to enforce tenant isolation or authorization.

4. Deletes apply the entity's global filter before deleting join records or file chunks. An excluded entity is left intact, together with its related records and chunks. With an active global filter and registered relationships, non-file deletes conditionally delete each candidate and use only the stored BSON IDs returned by successful deletions for relationship cleanup. If an entity becomes excluded after the eligibility read, it keeps its relationships. This applies to entity, direct-ID, ID-list, predicate, and filter overloads, including mixed batches where only some entities are deleted. File deletes retain their separate conditional metadata/reference checks.

   Filtered non-file deletion with registered relationships requires one conditional delete round trip per candidate, followed by batched relationship cleanup. Without registered relationships, it uses a bulk delete per batch, retaining the eligibility read and rechecking the global filter on the write. Unfiltered deletes, including calls with `IgnoreGlobalFilters = true`, retain the bulk-delete path.

   Initialize relationships before running concurrent deletes. The relationship registry is checked on each operation, not permanently cached as empty. If the bulk path has already been selected, a later registration does not add cleanup to that in-flight deletion. Surviving entities keep their relationships, but relationships registered concurrently for deleted entities can remain orphaned. Later deletes see the registration and use conditional deletion.

   Cascades are **not atomic across collections without a transaction**. Entities are deleted before relationship cleanup in the filtered non-file path. If a later entity delete or cleanup fails, the operation throws, but earlier deletes are not rolled back and orphaned join records may remain. Retrying an entity deletion is not guaranteed to repair those orphans. Use a caller-owned transaction when entity and relationship deletion must succeed or roll back together, and handle transaction conflicts by retrying the whole transaction as appropriate. The library uses your session without committing, aborting, or disposing it. This does not turn global filters into a database-level authorization boundary.
