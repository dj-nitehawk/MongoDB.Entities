# Connections
This category showcases the different methods of setting up the connections to mongodb.

### Connect to localhost
```csharp
await DB.InitAsync("db-name","localhost");
```

### Connect using a connection string
```csharp
await DB.InitAsync("db-name",
    MongoClientSettings.FromConnectionString(
        "mongodb+srv://user:password@cluster.mongodb.net/db-name"));
```

### Connect with a username & password
```csharp
await DB.InitAsync("db-name", new MongoClientSettings()
{
    Server = new MongoServerAddress("localhost", 27017),
    Credential = MongoCredential.CreateCredential("db-name", "username", "password")
});
```

### Increase connection pool limit
```csharp
await DB.InitAsync("db-name", new MongoClientSettings
{
    MinConnectionPoolSize = 25,
    MaxConnectionPoolSize = 250,
});
```
