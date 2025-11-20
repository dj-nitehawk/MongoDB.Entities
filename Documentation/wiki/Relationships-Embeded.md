# Embedded Relationships
> [!tip]
> If you are going to store more than a handful of entities within another entity, it is best to store them by reference as described in [this page](Relationships-Referenced.md).

## One-to-one

```csharp
var author = new Author { Name = "Eckhart Tolle" }
await author.SaveAsync();

book.Author = author;
await book.SaveAsync()
```
as mentioned earlier, calling `SaveAsync()` persists `author` to the "Authors" collection in the database. it is also stored in `book.Author` property. so, the `author` entity now lives in two locations (in the collection and also inside the `book` entity) and will have the same `ID`. if the goal is to embed something as an independant document, it is best to use a class that does not inherit from the `Entity` class or simply use the `.ToDocument()` method of an entity as explained earlier.

### Embed removal
to remove the embedded `author`, simply do:
```csharp
book.Author = null;
await book.SaveAsync();
```
the original `author` in the `Authors` collection is unaffected.

### Entity deletion
if you call `book.Author.DeleteAsync()`, the author entity is deleted from the `Authors` collection if it was a linked entity (has the same `ID`).

## One-to-many

```csharp
  book.OtherAuthors = new Author[] { author1, author2 };
  await book.SaveAsync();
```
### Embed removal:
```csharp
  book.OtherAuthors = null;
  await book.SaveAsync();
```
the original `author1, author2` entities in the `Authors` collection are unaffected.

### Entity deletion:
if you call `book.OtherAuthors.DeleteAllAsync()` the respective `author1, author2` entities are deleted from the `Authors` collection if they were linked entities (has the same `IDs`).
