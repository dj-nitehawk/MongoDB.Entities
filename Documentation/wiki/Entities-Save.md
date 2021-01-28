# Save an entity

call `SaveAsync()` on any entity to persist it to the database.

```csharp
var book = new Book { Title = "The Power Of Now" }; 
await book.SaveAsync();
```

> [!note]
> new entities are automatically assigned an `ID` when saved.
> saving an entity that has the `ID` already populated will replace the matching entity in the database.

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
you can also use the DB static class for saving entities like so:
```csharp
await DB.SaveAsync(book);
await DB.SaveAsync(books);
```

# Save entities partially
the above-mentioned `SaveAsync` methods will replace the entire document on the database with the values from the entity. if the goal is to only save the values of a subset of the properties, you have two choices to make your life easier.

### Save only a few specified properties
```csharp
await book.SaveOnlyAsync(x => new { x.Title, x.Price });
``` 
this will save **only** the Title and Price properties and exclude all other properties of the entity.

### Save all others except for the specified properties
```csharp
await book.SaveExceptAsync(x => new { x.AuthorName })
``` 
this will save all other properties of the entity **except** the `AuthorName` property.

> [!note] 
> you should only specify root level properties with the **New** expression. i.e. **x => x.Author.Name** is not valid. 

> [!tip]
> if the `ID` value of the entity being saved is `null`, a new document will be created in the database. if the `ID` has a value, then the matching document will be updated instead.

# Partial save with attributes
if you find specifying `New` expressions everywhere in your code as above tedious when needing to omit properties while saving an entity, you can use the `SavePreservingAsync()` method together with the use of an attribute. 

simply decorate the properties you want to omit with the \[[Preserve](xref:MongoDB.Entities.PreserveAttribute)\] attribute and call `book.SavePreservingAsync()` without having to supply an expression everytime. whatever properties you have decorated with `[Preserve]` attribute, will not be updated. all other properties of the entity will be updated with the values from your entity.

you can also do the opposite with the use of \[[DontPreserve](xref:MongoDB.Entities.DontPreserveAttribute)\] attribute. if you decorate properties with `[DontPreserve]`, only the values of those properties are written to the database and all other properties are implicitly ignored when calling `SavePreservingAsync()`.

> [!note]
> both **[DontPreserve]** and **[Preserve]** cannot be used together on the same entity type due to the conflicting nature of what they do.

# Embed an entity

to store an unlinked copy of an entity,  call the `ToDocument()` method. doing so will store an independant duplicate (with a new ID) of the original entity that has no relationship to the original entity.

```csharp
book.Author = author.ToDocument();
book.OtherAuthors = (new Author[] { author2, author3 }).ToDocuments();
await book.SaveAsync();
```