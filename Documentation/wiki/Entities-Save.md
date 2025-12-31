# Save an entity

Call `db.SaveAsync()` with any entity to persist it to the database.

```csharp
var book = new Book { Title = "The Power Of Now" };
await db.SaveAsync(book);
```

New entities are automatically assigned an `ID` when saved. Saving an entity that has the `ID` already populated will *[replace](Schema-Changes.md)* the matching entity in the database if it exists. If an entity with that `ID` does not exist in the database, a new one will be created.

## Save multiple entities

Multiple entities can be saved in a single bulk operation like so:

```csharp
var books = new[]
{
    new Book { Title = "Book One" },
    new Book { Title = "Book Two" },
    new Book { Title = "Book Three" }
};

await db.SaveAsync(books);
```

## Save entities partially

The above-mentioned `SaveAsync` methods will replace the entire document in the database with the values from the entity. If the goal is to only save the values of a subset of the properties, you have two convenient choices.

### Save only a few specified properties

```csharp
await db.SaveOnlyAsync(book, x => new { x.Title, x.Price });
``` 

This will save **only** the Title and Price properties and exclude all other properties of the entity.

### Save all others except for the specified properties

```csharp
await db.SaveExceptAsync(book, x => new { x.AuthorName });
``` 

This will save all other properties of the entity **except** the `AuthorName` property.

> [!note]
> you should only specify root level properties with the **New** expression. i.e. **x => x.Author.Name** is not valid.

> [!tip]
> if the `ID` value of the entity being saved is `null`, a new document will be created in the database. if the `ID` has a value, then the matching document will be updated instead.

## Partial save with attributes

If you find specifying `New` expressions everywhere in your code as above tedious when needing to omit properties while saving an entity, you can use the `SavePreservingAsync()` method together with the use of an attribute.

Simply decorate the properties you want to omit with the \[[Preserve](xref:MongoDB.Entities.PreserveAttribute)\] attribute and call `db.SavePreservingAsync(book)` without having to supply an expression everytime. Whatever properties you have decorated with `[Preserve]` attribute, will not be updated. All other properties of the entity will be updated with the values from your entity.

You can also do the opposite with the use of \[[DontPreserve](xref:MongoDB.Entities.DontPreserveAttribute)\] attribute. If you decorate properties with `[DontPreserve]`, only the values of those properties are written to the database and all other properties are implicitly ignored when calling `SavePreservingAsync()`.

> [!note]
> both **[DontPreserve]** and **[Preserve]** cannot be used together on the same entity type due to the conflicting nature of what they do.

## Inserts

Even though inserts can be handled with the `.SaveAsync()` methods above, you can also do inserts specifically using the `.InsertAsync()` methods like below:

```csharp
await db.InsertAsync(author);
await db.InsertAsync(authors);
```

## Embed an entity

To store an unlinked copy of an entity, call the `ToDocument()` method. Doing so will store an independent duplicate (with a new ID) of the original entity that has no relationship to the original entity.

```csharp
book.Author = author.ToDocument();
book.OtherAuthors = authors.ToDocuments();
await db.SaveAsync(book);
```