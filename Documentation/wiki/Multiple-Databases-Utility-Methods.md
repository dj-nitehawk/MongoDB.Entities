# Utility methods

## Get database name from an entity instance or type
```csharp
var dbName = pic.DatabaseName();
var dbName = DB.DatabaseName<Book>();
```
the above methods will return the name of the database that the entity is stored in. if not specifically attached to seperate db, it will return the name of the default database.

## Check if a database already exists on the server
```csharp
bool dbExists = await DB.Database("BookShopFILES").ExistsAsync();
bool dbExists = await DB.Database<Picture>().ExistsAsync();
```

## Check if a database is still accessible
```csharp
bool isAlive = await DB.Database("BookShopFILES").IsAccessibleAsync();
bool isAlive = await DB.Database<Picture>().IsAccessibleAsync();
```

## Get a list of all databases on the server
```csharp
var dbNames = await DB.AllDatabaseNamesAsync("localhost");
```
