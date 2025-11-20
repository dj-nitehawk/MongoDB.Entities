# Install

install the nuget package with command: 
```
Install-Package MongoDB.Entities
```

# Initialize

import the package with `using MongoDB.Entities;` and initialize the database connection as follows:

## Basic initialization
```csharp
var db = await DB.InitAsync("DatabaseName", "HostAddress", portNumber);
```

> [!note]
> The first database you initializes with the `DB.InitAsync` call becomes the `default` database of the application. You can retrieve the default database from anywhere in your code with the static property `DB.Default`. Once databases are initialized during startup with `DB.InitAsync`, you can retrieve an instance of any initialized database by supplying the name of the database with `DB.Instance("DatabaseName")`. If you have multiple `MongoClients` with the same database name, you can access that instance by supplying both the database name and the client settings like so: `DB.Instance("DatabaseName", clientSettings)`.  

## Advanced initialization
```csharp
var db = await DB.InitAsync("DatabaseName", new MongoClientSettings()
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
<!-- > these MongoClientSettings will only work for mongodb v4.0 or newer databases as it will use the `SCRAM-SHA-256` authentication method. if your db version is older than that and uses `SCRAM-SHA-1` authentication method, please [click here](https://gist.github.com/dj-nitehawk/a0b1484dbba90085305520c156502608) to see how to connect or you may use a connection string to connect as shown below. -->

## Using a connection string
```csharp
var db = await DB.InitAsync("DatabaseName",
  MongoClientSettings.FromConnectionString(
    "mongodb://{username}:{password}@{hostname}:{port}/?authSource=admin"));
```