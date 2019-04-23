# MongoDAL
A data access library for MongoDB with an elegant api, LINQ support and built-in entity relationship management.



## Install

install the nuget package with command: 
```
Install-Package MongoDAL
```



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
        Credential = 
            MongoCredential.CreateCredential("DatabaseName", "UserName", "Password")
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

#### Creating entities:

create your entities by inheriting the `Entity` base class.

```csharp
    public class Book : Entity
    {
        public string Title { get; set; }
    }
```
#### Ignoring properties:

if there are properties of entities you don't want persisted to mongodb, simply use the `IgnoreAttribute` 
```csharp
    public class Book : Entity
    {
    	[Ignore]
        public string SomeProperty { get; set; }
    }
```

#### Saving entities:

simply call `Save()` on any entity to save the changes. new entities are automatically assigned an `ID` when they are persisted to the database.

```csharp
	var book = new Book { Title = "The Power Of Now" }; 
	book.Save();
```

#### Embedding entities as documents:

to store an unlinked copy of an entity,  call the `ToDocument()` method. doing so will guarantee it to be a unique copy of the entity that is not linked to anything else in the database.

```csharp
	book.Author = author.ToDocument();	
	book.OtherAuthors = (new Author[] { author2, author3 }).ToDocument();
```



## Relationships (Embedded)

#### One-to-one:

```csharp
    var author = new Author { Name = "Eckhart Tolle" }
	author.Save();
    book.Author = author;
	book.Save()
```

as mentioned earlier, calling `Save()` persists `author` to the "Authors" collection in the database. it is also assigned to a property of the `book`. the `author` entity now lives in two locations (in the collection and also in the `book` entity) and both are linked by the `ID`.  you could also embed the `author` by not calling `author.Save()` in order to embed it in an unlinked state.

#### One-to-many:

```csharp
    book.OtherAuthors = new Author[] { author1, author2 };
    book.Save();
```



## Relationships (Referenced)

referenced relationships require a bit of special handling. a **one-to-one** relationship is defined by using the `One<Book,Author>` class and **one-to-many** relationships are defined by using the `Many<Book,Author>` class. it is also a good idea to initialize the `Many<>` properties with the `Initialize()` method as shown below in order to avoid null-reference exceptions during runtime.

```csharp
    public class Book : Entity
    {
        public One<Author> MainAuthor { get; set; }
        public Many<Book,Author> Authors { get; set; }
        
        public Book() => Authors = Authors.Initialize(this);
    }
```

#### One-to-one:

call the `ToReference()` method of the entity you want to store as a reference like so:

```csharp
    book.MainAuthor = author.ToReference();
    book.Save();
```

#### One-to-many:

```charp
	book.Authors.Add(author);
```

there's no need to call `book.Save()` because references are automatically created and saved using special joining collections in the form of `Book_Author` in the database. you don't have to pay any attention to these special collections unless you rename your entities. for ex: if you rename the `Book` entity to `AwesomeBook` just rename the corresponding join table from `Book_Author` to `AwesomeBook_Author` in order to get the references working again.

#### ToEntity() shortcut:

a reference can be turned back in to an entity with the `ToEntity()` method.

```
	var author = book.MainAuthor.ToEntity();
	var author = book.Authors.Collection().FirstOrDefault().ToEntity();
```



## Queries

#### Main collections:

```csharp
    var author = (from a in DB.Collection<Author>()
                  where a.Name.Contains("Eckhart")
                  select a).FirstOrDefault();
```

#### Reference collections:

```csharp
    var authors = (from a in book.Authors.Collection()
                   select a).ToArray();
```

#### Shortcut for collections:

```csharp
    var result = from a in author.Collection()
                 select a;
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



## Schema Changes



## Examples


