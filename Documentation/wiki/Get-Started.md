# Install

install the nuget package with command: 
```
Install-Package MongoDB.Entities
```

# Initialize

first import the package with `using MongoDB.Entities;`

then initialize the database connection like so:

## Basic initialization
```csharp
await DB.InitAsync("DatabaseName", "HostAddress", PortNumber);
```

## Advanced initialization
```csharp
await DB.InitAsync(new MongoClientSettings()
{
    Server = new MongoServerAddress("localhost", 27017),
    Credential = MongoCredential.CreateCredential("DatabaseName", "username", "password")
}, 
"DatabaseName");
```

## Using a connection string
```csharp
await DB.InitAsync(MongoClientSettings.FromConnectionString(
      "mongodb+srv://user:password@cluster.mongodb.net/DatabaseName"), 
      "DatabaseName");
```