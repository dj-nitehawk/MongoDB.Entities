# Global filters

With the use of global filters you can set up a set of criteria to be applied to all operations performed by a `DB` instance in order to save the trouble of having to specify the same criteria in each and every operation you perform. I.e. you specify common criteria in one place, and all **retrieval, update and delete** operations will have the common filters automatically applied to them before execution.

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
db.IgnoreGlobalFilters = true; //all operations ignore global filters until changed
await db.DeleteAsync<Book>(b => b.Title == "Power Of Tomorrow");
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

3. Only delete, update and retrieval operations will use global filters. The `Save*()` operations will ignore any registered global filters as they will only match on the `ID` field.