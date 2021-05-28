# Get distinct values of a property
you can get a list of unique values for a given property of an entity with the `Distinct<T,TProperty>()` method of the `DB` entrypoint.

`T` is the type of entity you want to query.
`TProperty` is the type of the property whos unique values you want returned.

### Get a list of all distinct values
```csharp
var genres = await DB.Distinct<Book, string>()
                     .Property(b => b.Genre)
                     .ExecuteAsync();
```
use `.Property()` to specify the property you want to get the unique values of, and finally call the `.ExecuteAsync()` method.

### Get distinct values for a subset of entities
```csharp
var genres = await DB.Distinct<Book, string>()
                     .Property(b => b.Genre)
                     .Match(b => b.AuthorName == "Eckhart Tolle")
                     .ExecuteAsync();
```
use `.Match()` to specify the filter criteria. There are other overloads similar to the `DB.Find().Match()` method which you can use to filter the data.
you can also call `.Match()` multiple times to build an `And` filter.

