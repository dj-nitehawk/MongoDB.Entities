# Automatic audit fields

Rather than setting audit values manually for every save/update operation, you can obtain a `DB` instance initialized with the current user’s audit data. The library will then populate audit fields on entities automatically during write operations.

## Enable audit fields

Simply add a property of type `ModifiedBy` to the entity class where you'd like to enable audit fields. The `ModifiedBy` type is provided by the library. It can be inherited and other properties can be added to it as you please.

```csharp
public class Book : Entity
{
    public string Title { get; set; }
    public ModifiedBy ModifiedBy { get; set; }
}
```

## Obtain a DB instance

Retrieve a database instance by providing it a `ModifiedBy` instance with the current user's details filled in.

```csharp
var currentUser = new ModifiedBy
{
    UserID = "xxxxxxxxxxxx",
    UserName = "Kip Jennings"
};

var db = DB.Instance("SomeDatabase")
           .WithModifiedBy(currentUser);

// if you already have a DB instance
db = db.WithModifiedBy(currentUser);
```

## Perform entity operations

In order for the auto audit fields to work, you must use the exact `DB` instance that was enriched with the audit values to perform the operations.

```csharp
db = db.WithModifiedBy(currentUser);
await db.SaveAsync(book);
await db.Update<Book>()
        .MatchID(book.ID)
        .Modify(b => b.Title, "updated title")
        .ExecuteAsync();
```

Doing so will result in the following document in mongodb:

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

## Get audit values

The audit values of a `DB` instance can be read like so:

```csharp
var currentUser = db.ModifiedBy;
```

## Transaction support

A transaction can be started with audit field support by creating a transaction on an instance that already has the audit values set.

```csharp
var db = DB.Instance("SomeDatabase").WithModifiedBy(currentUser);

using (var tn = db.Transaction())
{
    await tn.SaveAsync(book);
    await tn.CommitAsync();
}
```

> [!NOTE]
> please refer to the [transactions page](Transactions.md) for a detailed explanation of how transactions work.