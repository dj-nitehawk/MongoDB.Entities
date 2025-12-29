# ACID compliant transactions

multi-document transactions are performed like the following:

```csharp
var book1 = new Book { Title = "book one" };
var book2 = new Book { Title = "book two" };

await db.SaveAsync(new[] { book1, book2 });

using (var t = db.Transaction())
{
    var author1 = new Author { Name = "one" };
    var author2 = new Author { Name = "two" };

    await t.SaveAsync(new[] { author1, author2 });

    await t.DeleteAsync<Book>(new[] { book1.ID, book2.ID });

    await t.CommitAsync();
}
```

in the above code, book1 and book2 are saved before the transaction begins. author1 and author2 are created within the transaction and book1 and book2 are deleted within the transaction.

a transaction is started when you instantiate a `Transaction` object via `db.Transaction()`. you then perform all transaction logic using the methods supplied by that class such as `.SaveAsync()`, `.DeleteAsync()`, `.Update()`, `.Find()` instead of the `db` instance that created the transaction.

whatever transactional operations you do are only saved to the database once you call the `.CommitAsync()` method. if you do not call .CommitAsync(), then nothing changes in the database.

if an exception occurs before the .CommitAsync() line is reached, all changes are rolled back and the transaction is implicitly terminated.

it is best to always wrap the transaction in a `using statement` because reaching the end of the using statement will automatically end the transaction and dispose the underlying session. if no using statement is used, you will have to manually dispose the transaction object you created in order to finalize things.

you can also call `.AbortAsync()` to abort a transaction prematurely if needed at which point all changes will be rolled back.

## Relationship Manipulation

[relationships](Relationships-Referenced.md) within a transaction requires passing down the session to the `.Add()` and `.Remove()` methods as shown below.

```csharp
using (var t = db.Transaction())
{
    var author = new Author { Name = "author one" };
    await t.SaveAsync(author);

    var book = new Book { Title = "book one" };
    await t.SaveAsync(book);

    await author.Books.AddAsync(book, t.Session);
    await author.Books.RemoveAsync(book, t.Session);

    await t.CommitAsync();
}
```

## File Storage

[file storage](File-Storage.md) within a transaction also requires passing down the session like so:

```csharp
using (var t = db.Transaction())
{
    var picture = new Picture { Title = "my picture" };
    await t.SaveAsync(picture);

    var streamTask = new HttpClient()
        .GetStreamAsync("https://placekitten.com/g/4000/4000");

    using (var stream = await streamTask)
    {
        await picture.Data(db).UploadAsync(stream, session: t.Session);
    }

    await t.CommitAsync();
}
```