
# MongoDAL
An easy to use Data Access Layer for .Net Core applications to use LINQ with MongoDB.

## How To Use
---

## Install
install nuget package with the command: `Install-Package MongoDAL` 

or use the nuget package manager and search for `MongoDAL`

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

## Save Entities (create/update)
you can persist an entity to mongodb by calling the `Save<T>` method of the `DB` static class. there's no need to initialize the `DB` class.
```csharp
    var address = new Address {
        Street = "123 Street",
        City = "Colarado",
        Country = "USA" };
        
    DB.Save<Address>(address);
```
once saved to mongodb, you can access the auto generated `Id` of the entity like so:
```csharp
   var id = address.Id;
```
when updating entities, if an entity with a matching `Id` is found in the database, it will be overwritten with the entity you supply to the `Save<T>()` method. so you have to be mindful of schema changes to your entities in order to avoid data loss.

## Find Entities
linq queries can be written against any entity collection in mongodb like so:
```csharp
    var myAddress = (from a in DB.Collection<Address>()
                     where a.City.Contains("123")
                     select a).SingleOrDefault();
```
most linq operations are available. check out the mongodb [c# driver linq documentation](http://mongodb.github.io/mongo-csharp-driver/2.7/reference/driver/crud/linq/) for more details.

## Delete Entities
single entities can be deleted by supplying the `Id` of the entity to delete. multiple entities can be deleted by supplying a lamba expression as shown below:
```csharp
    DB.Delete<Address>(myAddress.Id);
    DB.DeleteMany<Address>(a => a.Country.Equals("USA"));
```
## Entity Relationships
entities can be embedded within entities or can be referenced by their `Id`.
#### Embedded Entities
```csharp
    public class Person : MongoEntity
    {
        public string Name { get; set; }
        public Address HomeAddress { get; set; }
     }
```
#### Referenced Entities
decorate properties you want treated as references with the `MongoRef` attribute. to mark a collection of Ids, simply store them in a string[].
```csharp
    public class Person : MongoEntity
    {
        public string Name { get; set; }
        
		[MongoRef]
		public string AddressId { get; set; }
		
		[MongoRef]
		public string[] VehicleIDs { get; set; }
     }
```

## Ignoring Entity Properties
if there are properties of your entities that you don't want persisted to mongodb, simply use the `MongoIgnoreAttribute` like so:
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

## Examples
[click here](https://github.com/dj-nitehawk/MongoDAL/blob/master/DemoAPI/Controllers/DemoController.cs) for basic examples.

for more in-depth examples, check the ASPNetCore-WebAPI project [here](https://github.com/dj-nitehawk/KiwilinkCRM/tree/master/Kiwilink-API).
