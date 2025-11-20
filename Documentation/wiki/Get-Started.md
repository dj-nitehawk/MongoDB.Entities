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
var defaultDB = await DB.InitAsync("DatabaseName", "HostAddress", PortNumber);
```
The first call to ```DB.InitAsync``` will set the default database and can be accessed with the returned instance or ```DB.Instance()```.

Subsequent calls to ```DB.InitAsync``` can be accessed with the returned instance or ```DB.Instance("DatabaseName")```

If you have multiple MongoClients with the same database name, you can access that instance by supplying both the database name and the client settings, i.e. ```DB.Instance("DatabaseName", clientSettings)```.  

## Advanced initialization```DB.InitAsync```
```csharp
await DB.InitAsync("DatabaseName", new MongoClientSettings()
    {
        Server = new MongoServerAddress("localhost", 27017),
        Credential = MongoCredential.CreateCredential("DatabaseName", "username", "password")
    },
    new MongoDatabaseSettings()
        {
            ReadConcern = ReadConcern.Majority,
            WriteConcern = WriteConcern.WMajority
        });
```
> these MongoClientSettings will only work for mongodb v4.0 or newer databases as it will use the `SCRAM-SHA-256` authentication method. if your db version is older than that and uses `SCRAM-SHA-1` authentication method, please [click here](https://gist.github.com/dj-nitehawk/a0b1484dbba90085305520c156502608) to see how to connect or you may use a connection string to connect as shown below.

## Using a connection string
```csharp
await DB.InitAsync("DatabaseName",
    MongoClientSettings.FromConnectionString(
        "mongodb://{username}:{password}@{hostname}:{port}/?authSource=admin"));
```