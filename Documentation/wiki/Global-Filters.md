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

4. Direct-ID deletes apply the entity's global filter before deleting join records or file chunks. An excluded entity is left intact, together with its related records and chunks.
