# ACID compliant transactions

Multi-document transactions are performed like the following:

```csharp
var book1 = new Book { Title = "book one" };
var book2 = new Book { Title = "book two" };

await db.SaveAsync(new[] { book1, book2 });

using (var tx = db.Transaction())
{
    var author1 = new Author { Name = "one" };
    var author2 = new Author { Name = "two" };

    await tx.SaveAsync(new[] { author1, author2 });

    await tx.DeleteAsync<Book>(new[] { book1.ID, book2.ID });

    await tx.CommitAsync();
}
```

In the above code, book1 and book2 are saved before the transaction begins. Author1 and author2 are created within the transaction and book1 and book2 are deleted within the transaction.

A transaction is started when you instantiate a `Transaction` object via `db.Transaction()`. You then perform all transaction logic using the methods supplied by that class such as `.SaveAsync()`, `.DeleteAsync()`, `.Update()`, `.Find()` instead of the `db` instance that created the transaction.

Whatever transactional operations you do are only saved to the database once you call the `.CommitAsync()` method. If you do not call .CommitAsync(), then nothing changes in the database.

If an exception occurs before the .CommitAsync() line is reached, all changes are rolled back and the transaction is implicitly terminated.

It is best to always wrap the transaction in a `using statement` because reaching the end of the using statement will automatically end the transaction and dispose the underlying session. If no using statement is used, you will have to manually dispose the transaction object you created in order to finalize things.

You can also call `.AbortAsync()` to abort a transaction prematurely if needed at which point all changes will be rolled back.

## Relationship Manipulation

[relationships](Relationships-Referenced.md) within a transaction requires passing down the session to the `.Add()` and `.Remove()` methods as shown below.

```csharp
using (var tx = db.Transaction())
{
    var author = new Author { Name = "author one" };
    await tx.SaveAsync(author);

    var book = new Book { Title = "book one" };
    await tx.SaveAsync(book);

    await author.Books.AddAsync(book, tx.Session);
    await author.Books.RemoveAsync(book, tx.Session);

    await tx.CommitAsync();
}
```

## File Storage

[file storage](File-Storage.md) within a transaction also requires passing down the session like so:

```csharp
using (var tx = db.Transaction())
{
    var picture = new Picture { Title = "my picture" };
    await tx.SaveAsync(picture);

    var streamTask = new HttpClient()
        .GetStreamAsync("https://placekitten.com/g/4000/4000");

    using (var stream = await streamTask)
    {
        await picture.Data(db).UploadAsync(stream, session: tx.Session);
    }

    await tx.CommitAsync();
}
```