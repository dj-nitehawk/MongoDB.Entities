# Automatic audit fields
instead of setting the audit values manually on each and every save or update operation, you can take advantage of a `DBContext` instance where you will instantiate the context by prodiving it with the details of the current user perfoming the operations once, and then use the db context to perform all subsequesnt save/update operations so that all the audit fields will be set on the entities automatically.

## Enable audit fields
simply add a property of type `ModifiedBy` to the entity class where you'd like to enable audit fields. The `ModifiedBy` type is provided by the library. It can be inherited and other properties can be added to it as you please.

```csharp
public class Book : Entity
{
    public string Title { get; set; }
    public ModifiedBy ModifiedBy { get; set; }
}
```

## Instantiate a DBContext
instantiate a context by providing it a `ModifiedBy` instance with the current user's details filled in.
```csharp
var currentUser = new ModifiedBy
{
    UserID = "xxxxxxxxxxxx",
    UserName = "Kip Jennings"
};

var db = new DBContext(modifiedBy: currentUser);
```

## Perform entity operations
in order for the auto audit fields to work, you must use the db context to perform the operations instead of the `DB` static methods like you'd typically use.
```csharp
var book = new Book { Title = "test book" };

await db.SaveAsync(book);

await db.Update<Book>()
        .MatchID(book.ID)
        .Modify(b => b.Title, "updated title")
        .ExecuteAsync();
```

doing so will result in the following document in mongodb:
```
{
	"_id" : ObjectId("xxxxxxxxxxxx"),
	"Title" : "updated title", //this will initially be 'test book'
	"ModifiedBy" : {
		"UserID" : "xxxxxxxxxxxx",
		"UserName" : "Kip Jennings"
	}
}
```

## Get/set audit values
it is also possible to instantiate a `DBContext` without supplying a `ModifiedBy` to the constructor and set or get it like so:
```csharp
var dbContext = new DBContext();

db.ModifiedBy = new ModifiedBy
{
    UserID = "xxxxxxxxxxxx",
    UserName = "Kip Jennings"
};

var currentUser = db.ModifiedBy;
```

## Transaction support
a transaction with audit field support can be performed with the DBContext like so:
```csharp
var db = new DBContext(modifiedBy: currentUser);

using (db.Transaction())
{
    await db.SaveAsync(book);
    await db.CommitAsync();
}
```

or it can be performed with a `Transaction` instance like so:
```csharp
using (var TN = DB.Transaction(modifiedBy: currentUser))
{
    await TN.SaveAsync(book);
    await TN.CommitAsync();
}
```
> [!NOTE]
> please refer to the [transactions page](Transactions.md) for a detailed explanation of how transactions work.
