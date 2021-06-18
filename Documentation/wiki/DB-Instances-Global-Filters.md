# Global filters

with the use of global filters you can specify a set of criteria to be applied to all operations performed by a `DBContext` instance in order to save the trouble of having to specify the same criteria on each and every operation you perform. i.e. you specify common criteria in one place, and all **retrieval, update and delete** operations will have the common filters automatically applied to them before execution.

to be able to specify common criteria, you need to create a derived `DBContext` class just like with the event hooks.

```csharp
public class MyDBContext : DBContext
{
    public MyDBContext()
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
## Specify filters using a base class
filters can be specified on a per entity type basis like above or common filters can be specified using a base class type like so:

```csharp
SetGlobalFilterForBaseClass<BaseEntity>(x => x.IsDeleted == false);
```

## Prepending global filters
global filters by deafult are appeneded to your operation filters. if you'd like to instead have the global filters prepended, use the following overload:

```csharp
SetGlobalFilter<Book>(
    filter: b => b.Publisher == "Harper Collins",
    prepend: true);
```

## Temporarily ignoring global filters
it's possible to skip/ignore global filters on a per operation basis as follows:
```csharp
//with command builders:
await db.Find<Book>()
        .Match(b => b.Title == "Power Of Tomorrow")
        .IgnoreGlobalFilters()
        .ExecuteAsync();

//with direct methods:
await db.DeleteAsync<Book>(
    b => b.Title == "Power Of Tomorrow",
    ignoreGlobalFilters: true);
```

## Limitations

1. only one filter per entity type is allowed. specify multiple criteria for the same entity type with the `&&` operator as shown above. if you call `SetGlobalFilter<Book>` more than once, only the last call will be registered.

2. if using a base class to specify filters, no derived entity type (of that specific base class) can be used for registering another filter. take the following for example:
```csharp
    SetGlobalFilter<Book>(b => b.Publisher == "Harper Collins");

    SetGlobalFilterForBaseClass<BaseEntity>(x => x.IsDeleted == false);
```
only the second filter would take effect. the first one is discarded because the `Book` type is a derived type of `BaseEntity`.

you can however switch the order of registration so that the base class registration occurs first. but you need to make sure to include the criteria the base class registration caters to as well, like so:
```csharp
    SetGlobalFilterForBaseClass<BaseEntity>(x => x.IsDeleted == false);

    SetGlobalFilter<Book>(
          b => b.Publisher == "Harper Collins" &&
               b.IsDeleted == false);
``` 

3. only delete, update and retrieval operations will use global filters. the `Save*()` operations will ignore any registered global filters as they will only match on the `ID` field.
