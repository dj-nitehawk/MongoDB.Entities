
# MongoDAL
A data access library for MongoDB with an elegant api, full LINQ support and built-in entity relationship management.

## Install
install nuget package with the command: 
```
Install-Package MongoDAL

## Initialize
first import the package with `using MongoDAL;`

then initialize as below according to the platform you're using.

#### ASP.Net Core (Basic initialization):
add the following inside `ConfigureServices` method of `Startup.cs` file:
```csharp
  services.AddMongoDAL("DatabaseName","HostAddress","PortNumber");
```

#### ASP.Net Core (Advanced initialization):
add the following inside `ConfigureServices` method of `Startup.cs` file:
```csharp
  services.AddMongoDAL(
      new MongoClientSettings()
      {
        Server = new MongoServerAddress("HostAddress", "PortNumber"),
        Credential = MongoCredential.CreateCredential("DatabaseName", "UserName", "Password")
       },
       "DatabaseName");
```

#### .Net Core (Basic initialization):
```csharp
  new DB("Demo");
```

#### .Net Core (Advanced initialization):
```csharp
  new MongoDAL.DB(new MongoClientSettings()
  {
      Server = new MongoServerAddress("localhost", 27017),
      Credential = MongoCredential.CreateCredential("Demo", "username", "password")
  }, "Demo");
```

## Entities
create your entities by inheriting from `MongoEntity` like so:
```csharp
    public class Address : MongoEntity
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
```
#### Ignoring Entity Properties
if there are properties of entities that you don't want persisted to mongodb, simply use the `MongoIgnoreAttribute` like so:
```csharp
    public class Address : MongoEntity
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        
        [MongoIgnore]
        public string SomeProperty { get; set; }
    }
```

## Async Support
async overloads are available for all provided methods.

in order to write async queries against collections, make sure to import the mongodb linq extensions and write queries as follows:
```csharp
using MongoDB.Driver;
using MongoDB.Driver.Linq;
```
```csharp
  var lastPerson = await (from p in DB.Collection<Person>()
                          orderby p.ModifiedOn descending
                          select p).FirstOrDefaultAsync();
```

## Examples
[click here](https://github.com/dj-nitehawk/MongoDAL/blob/master/DemoConsole/Program.cs) for basic examples.

for more in-depth examples, check the ASPNetCore-WebAPI project [here](https://github.com/dj-nitehawk/KiwilinkCRM/tree/master/Kiwilink-API).
