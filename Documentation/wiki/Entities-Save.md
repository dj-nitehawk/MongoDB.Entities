# Save an entity

call `SaveAsync()` on any entity to save the changes. new entities are automatically assigned an `ID` when they are persisted to the database. 

```csharp
var book = new Book { Title = "The Power Of Now" }; 
await book.SaveAsync();
```

> [!note]
> the ID property comes from the [Entity](xref:MongoDB.Entities.Entity) base class. 

# Save multiple entities

multiple entities can be saved in a single bulk operation like so:
```csharp
var books = new[] {
    new Book{ Title = "Book One" },
    new Book{ Title = "Book Two" },
    new Book{ Title = "Book Three"}
    };

await books.SaveAsync();
```

# Save via DB static class
you can use the DB static class for saving entities like so:
```csharp
await DB.SaveAsync(book);
await DB.SaveAsync(books);
```

# Partial save
## Partial save with new expression
if you'd like to skip one or more properties while saving a complete entity, you can do so with the `SavePreservingAsync()` method.
```csharp
await book.SavePreservingAsync(x => new { x.Title, x.Price })
```
this method will build an update command dynamically using reflection and omit the properties you specify. all other properties will be updated in the database with the values from your entity. sometimes, this would be preferable to specifying each and every property with an [update command](Entities-Update.md).

> [!note] 
> you should only specify root level properties with the **New** expression. i.e. **x => x.Author.Name** is not valid.

## Partial save with attribute
you can decorate the properties you want to omit with the \[[Preserve](xref:MongoDB.Entities.PreserveAttribute)\] attribute and simply call `book.SavePreservingAsync()` without supplying an expression. if you specify ommissions using both an expression and attributes, the expression will take precedence and the attributes are ignored.

you can also do the opposite with the use of \[[DontPreserve](xref:MongoDB.Entities.DontPreserveAttribute)\] attribute. if you decorate properties with `[DontPreserve]`, only the values of those properties are written to the database and all other properties are implicitly ignored when calling `SavePreservingAsync()`. also, the same rule applies that attributes are ignored if you supply a `new` expression to `SavePreservingAsync()`.

> [!note]
> both **[DontPreserve]** and **[Preserve]** cannot be used together on the same entity type due to the conflicting nature of what they do.

# Embed an entity

to store an unlinked copy of an entity,  call the `ToDocument()` method. doing so will store an independant duplicate (with a new ID) of the original entity that has no relationship to the original entity.

```csharp
book.Author = author.ToDocument();
book.OtherAuthors = (new Author[] { author2, author3 }).ToDocuments();
await book.SaveAsync();
```