# Embedded Relationships

> [!tip]
> If you are going to store more than a handful of entities within another entity, it is best to store them by reference as described in [this page](Relationships-Referenced.md).

## One-to-one

```csharp
var author = new Author { Name = "Eckhart Tolle" };
await db.SaveAsync(author);

book.Author = author;
await db.SaveAsync(book);
```

as mentioned earlier, calling `SaveAsync()` persists `author` to the "Authors" collection in the database. it is also stored in `book.Author` property. so, the `author` entity now lives in two locations (in the collection and also inside the `book` entity) and will have the same `ID`. if the goal is to embed something as an independent document, it is best to use a class that does not inherit from the `Entity` class or simply use the `.ToDocument()` method of an entity as explained earlier.

### Embed removal

to remove the embedded `author`, simply do:

```csharp
book.Author = null!;
await db.SaveAsync(book);
```

the original `author` in the `Authors` collection is unaffected.

## One-to-many

```csharp
book.OtherAuthors = [author1, author2];
await db.SaveAsync(book);
```

### Embed removal:

```csharp
book.OtherAuthors = null!;
await db.SaveAsync(book);
```

the original `author1, author2` entities in the `Authors` collection are unaffected.