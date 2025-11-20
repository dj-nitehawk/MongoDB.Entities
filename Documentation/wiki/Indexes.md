# Index creation
use the `Index<T>()` method to define indexes as shown below. specify index keys by chaining calls to the `.Key()` method. compound indexes are created by calling the `.Key()` method multiple times.

 first parameter of the method is a lambda pointing to the property on your entity. second parameter specifies the type of key. finally chain in a call to `.CreateAsync()` to finish defining the index.

> [!tip]
> you should define your indexes at the startup of your application so they only run once at launch. alternatively you can define indexes in the static constructor of your entity classes. if an index exists for the specified config, your commands will just be ignored by the server.

## Text indexes
```csharp
await db.Index<Author>()
        .Key(a => a.Name, KeyType.Text)
        .Key(a => a.Surname, KeyType.Text)
        .CreateAsync();
```
if the field you want to index is nested within arrays or lists, specify an expression with a `[0]` index position like so:
```csharp
.Key(a => a.Books[0].Reviews[0].Content, KeyType.Text)
```
in order to index all text properties of an entity, you can create a wildcard text index as follows:
```csharp
.Key(a => a, KeyType.Text)
```
## Full-text search
you can do full text searches after defining a text index as described above with the following:
```csharp
await db.Find<Book>()
        .Match(Search.Full, "search term")
        .ExecuteAsync();
```
you can also start a fluent aggregation pipeline with a $text stage as follows:
```csharp
db.FluentTextSearch<Book>(Search.Full, "search term")
```
> [!tip]
> [_click here_](https://docs.mongodb.com/manual/reference/operator/query/text/#search-field) to see more info on how to do text searches for phrases, negations, any words, etc.

## Fuzzy-text search
in order to run a fuzzy text match, simply change the first parameter to `Search.Fuzzy` as shown here:
```csharp
await db.Find<Book>()
        .Match(Search.Fuzzy, "search term")
        .ExecuteAsync();
```
> [!note]
> fuzzy text searching requires a bit of special handling, please see [_here_](Indexes-Fuzzy-Text-Search.md) for detailed information.

## Other index types
use the same `Index<T>()` method as above but with the type parameters of the keys set to one of the following enum values:
- Ascending
- Descending
- Geo2D
- Geo2DSphere
- Hashed
- Wildcard

## Indexing options
To specify options for index creation, specify an action using the `.Option()` method before calling the `.CreateAsync()` method.
```csharp
await db.Index<Book>()
        .Key(x => x.Title, KeyType.Descending)
        .Option(o =>
        {
            o.Background = false;
            o.Unique = true;
        })
        .CreateAsync();
```

## Retrieve the name of created index
The `.CreateAsync()` method returns the name of the index that was created.
```csharp
var name = await db.Index<Book>()
                   .Key(x => x.Title, KeyType.Ascending)
                   .Key(x=> x.Price, KeyType.Descending)
                   .CreateAsync();              
```

## Delete an index by name
```csharp
await db.Index<Book>().DropAsync(name);
```

## Delete all indexes for an entity type
```csharp
await db.Index<Book>().DropAllAsync();
```