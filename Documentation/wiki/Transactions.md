
# ACID compliant transactions
multi-document transactions are performed as shown below:

```csharp
var book1 = new Book { Title = "book one" };
var book2 = new Book { Title = "book two" };

await DB.SaveAsync(new[] { book1, book2 });

using (var TN = DB.Transaction())
{
      var author1 = new Author { Name = "one" };
      var author2 = new Author { Name = "two" };

      await TN.SaveAsync(new[] { author1, author2 });

      await TN.DeleteAsync<Book>(new[] { book1.ID, book2.ID });

      await TN.CommitAsync();
}
```
in the above code, book1 and book2 are saved before the transaction begins. author1 and author2 is created within the transaction and book1 and book2 are deleted within the transaction.

a transaction is started when you instantiate a `Transaction` object either via the factory method `DB.Transaction()` or `new Transaction()`. you then perform all transaction logic using the methods supplied by that class such as `.SaveAsync()`, `.DeleteAsync()`, `.Update()`, `.Find()` instead of the methods supplied by the `DB` static class like you'd normally do.

the methods of the `DB` class also supports transactions but you would have to supply a `session` to each method call, which would be less convenient than using the `Transaction` class.

whatever transactional operations you do are only saved to the database once you call the `.CommitAsync()` method. if you do not call .CommitAsync(), then nothing changes in the database.

if an exception occurs before the .CommitAsync() line is reached, all changes are rolled back and the transaction is implicitly terminated.

it is best to always wrap the transaction in a using statement because reaching the end of the using statement will automatically end the transaction and dispose the underlying session. if no using statement is used, you will have to manually dispose the transaction object you created in order to finalize things.

you can also call `.AbortAsync()` to abort a transaction prematurely if needed at which point all changes will be rolled back.

## Relationship Manipulation
[relationships](Relationships-Referenced.md) within a transaction requires passing down the session to the `.Add()` and `.Remove()` methods as shown below.
```csharp
using (var TN = DB.Transaction())
{
    var author = new Author { Name = "author one" };
    await TN.SaveAsync(author);

    var book = new Book { Title = "book one" };
    await TN.SaveAsync(book);

    await author.Books.AddAsync(book, TN.Session);
    await author.Books.RemoveAsync(book, TN.Session);

    await TN.CommitAsync();
}
```

## File Storage
[file storage](File-Storage.md) within a transaction also requires passing down the session like so:
```csharp
using (var TN = DB.Transaction())
{
    var picture = new Picture { Title = "my picture" };
    await TN.SaveAsync(picture);

    var streamTask = new HttpClient()
                      .GetStreamAsync("https://placekitten.com/g/4000/4000");

    using (var stream = await streamTask)
    {
        await picture.Data.UploadAsync(stream, session: TN.Session);
    }

    await TN.CommitAsync();
}
```

## Multiple Databases

a transaction is always tied to a single database. you can specify which database to use for a transaction in a couple of ways.
```csharp
var TN = DB.Transaction("DatabaseName") // manually specify the database name
var TN = DB.Transaction<Book>() // gets the database from the entity type
```

if you try to perform an operation on an entity type that is not connected to the same database as the transaction, mongodb server will throw an exception.

> [!note]
> please read the page on [multiple databases](Multiple-Databases.md) to understand how multi-db support works.
