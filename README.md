![Nuget](https://img.shields.io/nuget/v/MongoDAL.svg) 
![Nuget](https://img.shields.io/nuget/dt/MongoDAL.svg)
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
  services.AddMongoDAL("DatabaseName","HostAddress",PortNumber);
```
#### ASP.Net Core (Advanced initialization):
add the following inside `ConfigureServices` method of `Startup.cs` file:
```csharp
  services.AddMongoDAL(
      new MongoClientSettings()
      {
        Server = 
            new MongoServerAddress("HostAddress", PortNumber),
        Credential = 
            MongoCredential.CreateCredential("DatabaseName", "UserName", "Password")
       },
       "DatabaseName");
```

#### .Net Core (Basic initialization):
```csharp
  new DB("bookshop");
```

#### .Net Core (Advanced initialization):
```csharp
  new MongoDAL.DB(new MongoClientSettings()
  {
      Server = 
          new MongoServerAddress("localhost", 27017),
      Credential = 
          MongoCredential.CreateCredential("bookshop", "username", "password")
  }, "bookshop");
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

#### Deleting entities:

```csharp
    book.OtherAuthors.DeleteAll()
    book.Delete();
```

to delete entities in bulk, use a lambda expression as follows:

```csharp
	DB.Delete<Book>(b => b.Title.Contains("Trump"));
```



## Relationships (Embedded)

#### One-to-one:

```csharp
    var author = new Author { Name = "Eckhart Tolle" }
    author.Save();
    book.Author = author;
    book.Save()
```

as mentioned earlier, calling `Save()` persists `author` to the "Authors" collection in the database. it is also assigned to a property of the `book`. the `author` entity now lives in two locations (in the collection and also in the `book` entity) and both are linked by the `ID`.  you could also embed the `author` by not calling `author.Save()` in order to embed it in an unlinked state with an `ID` value of `null`.

#### One-to-many:

```csharp
    book.OtherAuthors = new Author[] { author1, author2 };
    book.Save();
```

**Tip:** If you are going to store more than a handful of entities within another entity, it is best to store them as references as shown in the next section.

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
	book.Authors.Remove(author);
```

there's no need to call `book.Save()` because references are automatically created and saved using special joining collections in the form of `Book_Author` in the database. you don't have to pay any attention to these special collections unless you rename your entities. for ex: if you rename the `Book` entity to `AwesomeBook` just rename the corresponding join collection from `Book_Author` to `AwesomeBook_Author` in order to get the references working again. also if you delete an entity that is referenced somewhere in the database, all references pointing to that entity is automatically deleted.

#### ToEntity() shortcut:

a reference can be turned back in to an entity with the `ToEntity()` method.

```csharp
	var author = book.MainAuthor.ToEntity();
	var author = book.Authors.Collection().FirstOrDefault().ToEntity();
```



## Queries

data can be queried using LINQ or lambda expressions. most LINQ operations are available. see the mongodb [c# driver linq documentation](http://mongodb.github.io/mongo-csharp-driver/2.7/reference/driver/crud/linq/) for more details.

#### Entity collections:

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

the `.Collection()` method of entities and references return an `IQueryable` which you can write queries against.




## Async Support

async overloads are available for all provided methods.

in order to write async queries against collections, make sure to import the mongodb linq extensions and write queries as follows:
```csharp
    using MongoDB.Driver;
    using MongoDB.Driver.Linq;
```
```csharp
    var lastAuthor = await (from a in author.Collection()
                            orderby a.ModifiedOn descending
                            select a).FirstOrDefaultAsync();
```



## Schema Changes

be mindful when changing the schema of your entities. the documents/entities stored in mongodb are always overwritten with the current schema of you entities. for example:

###### Old schema:

```csharp
    public class Book : Entity
    {
        public string Title { get; set; }
        public int Price { get; set; }
    }
```

###### New schema:

```csharp
    public class Book : Entity
    {
        public string Title { get; set; }
        public int SellingPrice { get; set; }
    }
```

the data stored in `Price` will be lost if you do not manually handle the transfer the data from the old property to the new property upon saving.



## Examples

to see working examples please [click here](https://github.com/dj-nitehawk/MongoDAL/blob/Examples/Program.cs)

to see MongoDAL used in a real-world application, check the ASP.Net Core WebAPI project [click here](https://github.com/dj-nitehawk/KiwilinkCRM/tree/master/Kiwilink-API)



## Donations
if MongoDAL has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking the button below:
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)