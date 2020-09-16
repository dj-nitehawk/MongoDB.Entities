# Deleting entities

deleting entities can be achieved in any of the following ways:

```csharp  
    await book.DeleteAsync();
    await book.OtherAuthors.DeleteAllAsync();
    await DB.DeleteAsync<Author>("ID");
    await DB.DeleteAsync<Book>(new[] { "ID1", "ID2" });
    await DB.DeleteAsync<Book>(b => b.Title.Contains("Trump")); 
```