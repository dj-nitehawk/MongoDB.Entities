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


>**NOTE**: an entity type is tied to a specific database by calling the `DatabaseFor` method with the database name on startup. that entity type will always be stored in and retrieved from that specific database only. it is not possible to save a single entity type in multiple databases.

#### Get database name from an entity instance or type
```csharp
    var dbName = pic.DatabaseName();
    var dbName = DB.DatabaseName<Book>();
```
the above methods will return the name of the database that the entity is stored in. if not specifically attached to seperate db, it will return the name of the default database.

#### Limitations
- cross-database relationships with `Many<T>` is not supported.
- no cross-database joins/ look-ups as the driver doesn't support it.
- storing a single entity type in multiple datbases is not supported.

### Removal of DB instances in v20

if you've used an earlier verion than v20 of this library, you may have used DB instances for performing operations rather than using the static DB class as the entrypoint. In v20, the DB instance support was removed in order to simplify the codebase and optimize performance as that release was a major jump in version with breaking changes to the API.

DB instances were a neccessity before v13 in order to support multiple database access. that requirement was removed in v13 after re-architecting the library internals and it was optional to use DB instances up until v20.

it can be argued that DB instances are useful for dependency injection but, ideally you should be injecting your repositories (that wrap up the DB methods) into your controllers/ services, not the DB instances directly. if you don't need to unit test or plan to swap persistance technology at a future date, then there's no need to use dependency injection and you are free to do everything via the DB static methods.

it is however, recommended that you encapsulate all data access logic in repository or manager classes in order to isolate data persistance logic from your application logic.

> **TIP**: as an alternative, have a look at `vertical slice architecture` as done [here](https://github.com/dj-nitehawk/MongoWebApiStarter) for a far superior developer experience compared to the commonly used layerd+di+repositories mess.

### [Next Page >>](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/11.-Fuzzy-Text-Search)