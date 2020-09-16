# Saving entities

call `SaveAsync()` on any entity to save the changes. new entities are automatically assigned an `ID` when they are persisted to the database.

```csharp
var book = new Book { Title = "The Power Of Now" }; 
await book.SaveAsync();
```

multiple entities can be saved in a single bulk operation like so:
```csharp
var books = new[] {
    new Book{ Title = "Book One" },
    new Book{ Title = "Book Two" },
    new Book{ Title = "Book Three"}
    };

await books.SaveAsync();
```
alternatively, you can do the following:
```csharp
await DB.SaveAsync(book);
await DB.SaveAsync(books);
```

# Embedding entities

to store an unlinked copy of an entity,  call the `ToDocument()` method. doing so will store an independant duplicate (with a new ID) of the original entity that has no relationship to the original entity.

```csharp
book.Author = author.ToDocument();
book.OtherAuthors = (new Author[] { author2, author3 }).ToDocuments();
await book.SaveAsync();
```