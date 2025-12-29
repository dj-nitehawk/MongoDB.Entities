# Utility methods

## Get database name from a DB instance

```csharp
var dbName = db.DatabaseName();
```

## Check if a database already exists on the server

```csharp
bool dbExists = await db.Database().ExistsAsync();
```

## Check if a database is still accessible

```csharp
bool isAlive = await db.Database().IsAccessibleAsync();
```

## Get a list of all databases on the server

```csharp
var dbNames = await DB.AllDatabaseNamesAsync("localhost");
```