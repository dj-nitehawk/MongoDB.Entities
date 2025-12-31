# Get distinct values of a property

You can get a list of unique values for a given property of an entity with the `Distinct<T,TProperty>()` method.

`T` is the type of entity you want to query.
`TProperty` is the type of the property whose unique values you want returned.

## Get a list of all distinct values

```csharp
var genres = await db.Distinct<Book, string>()
                     .Property(b => b.Genre)
                     .ExecuteAsync();
```

Use `.Property()` to specify the property you want to get the unique values of, and finally call the `.ExecuteAsync()` method.

## Get distinct values for a subset of entities

```csharp
var genres = await db.Distinct<Book, string>()
                     .Property(b => b.Genre)
                     .Match(b => b.AuthorName == "Eckhart Tolle")
                     .ExecuteAsync();
```

Use `.Match()` to specify the filter criteria. There are other overloads similar to the `db.Find().Match()` method which you can use to filter the data.
You can also call `.Match()` multiple times to build an `And` filter.