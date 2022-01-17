# Multiple database support
you can store and retrieve Entities in multiple databases on either a single server or multiple servers. the only requirement is to have unique names for each database. the following example demonstrates how to use multiple databases.

### Usage example:

use the `DB.DatabaseFor<T>()` method to specify which database you want the Entities of a given type to be stored in. it is only neccessary to do that for the entities you want to store in a non-default database. the default database is the very first database your application initializes. all entities by default are stored in the default database unless specified otherwise using `DatabaseFor`.

as such, the `Book` entities will be stored in the "BookShop" database and the `Picture` entities are stored in the "BookShopFILES" database considering the following code.

```csharp
await DB.InitAsync("BookShop");
await DB.InitAsync("BookShopFILES");

DB.DatabaseFor<Picture>("BookShopFILES");

var book = new Book { Title = "Power Of Now" };
await book.SaveAsync();
//alternative:
//// await DB.SaveAsync(book);

var pic = new Picture
{
    BookID = book.ID,
    Name = "Power Of Now Cover Photo"
};

await pic.SaveAsync();
//alternative:
//// await DB.SaveAsync(pic);

await DB.Update<Picture>()
        .Match(p => p.ID == pic.ID)
        .Modify(p => p.Name, "Updated Cover Photo")
        .ExecuteAsync();

var result = await DB.Find<Picture>().OneAsync(pic.ID);                    
```

> [!note]
> an entity type is tied to a specific database by calling the **DatabaseFor** method with the database name on startup. that entity type will always be stored in and retrieved from that specific database only. it is not possible to save a single entity type in multiple databases.

if you prefer to keep your database specifications inside the entity classes themselves, you could even call `DatabaseFor` in the static constructor like so:
```csharp
public class Picture : Entity
{
    static Picture() => DB.DatabaseFor<Picture>("BookShopFILES");
}
```
### Limitations
- cross-database relationships with `Many<T>` is not supported.
- no cross-database joins/ look-ups as the driver doesn't support it.
- storing a single entity type in multiple datbases is not supported.