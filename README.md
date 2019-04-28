[![](https://img.shields.io/nuget/v/MongoDB.Entities.svg)](#) [![](https://img.shields.io/nuget/dt/MongoDB.Entities.svg)](#) [![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)
# MongoDB.Entities
It is a data access library for MongoDB with an elegant api, LINQ support and built-in entity relationship management features. The library aims to tuck away the complexity of the official C# driver and help fast-track application development with MongoDB. Both ***Document*** and ***Relational*** type data modelling is possible. Developers coming from an Entity Framework background will feel right at home.



## Install

install the nuget package with command: 
```
Install-Package MongoDB.Entities
```



## Initialize

first import the package with `using MongoDB.Entities;`

then initialize as below according to the platform you're using.

#### ASP.Net Core (Basic initialization):
add the following inside `ConfigureServices` method of `Startup.cs` file:
```csharp
  services.AddMongoDBEntities("DatabaseName","HostAddress",PortNumber);
```
#### ASP.Net Core (Advanced initialization):
add the following inside `ConfigureServices` method of `Startup.cs` file:
```csharp
  services.AddMongoDBEntities(
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
  new DB(new MongoClientSettings()
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

call `Save()` on any entity to save the changes. new entities are automatically assigned an `ID` when they are persisted to the database.

```csharp
    var book = new Book { Title = "The Power Of Now" }; 
    book.Save();
```

#### Embedding entities as documents:

to store an unlinked copy of an entity,  call the `ToDocument()` method. doing so will store an independant duplicate of the original entity that has no relationship to the original entity.

```csharp
    book.Author = author.ToDocument();
    book.OtherAuthors = (new Author[] { author2, author3 }).ToDocuments();
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
as mentioned earlier, calling `Save()` persists `author` to the "Authors" collection in the database. it is also stored in `book.Author` property. so, the `author` entity now lives in two locations (in the collection and also inside the `book` entity) and are linked by the `ID`. if the goal is to embed something as an independant/unlinked document, it is best to use a class that does not inherit from the `Entity` class or simply use the `.ToDocument()` method of an entity as explained earlier.

###### Embed Removal:
to remove the embedded `author`, simply do:
```csharp
	book.Author = null;
	book.Save();
```
the original `author` in the `Authors` collection is unaffected.

###### Entity Deletion:
if you call `book.Author.Delete()`, the author entity is deleted from the `Authors` collection if it was a linked entity.

#### One-to-many:

```csharp
    book.OtherAuthors = new Author[] { author1, author2 };
    book.Save();
```
**Tip:** If you are going to store more than a handful of entities within another entity, it is best to store them by reference as described below.

###### Embed Removal:
```csharp
    book.OtherAuthors = null;
    book.Save();
```
the original `author` entities in the `Authors` collection are unaffected.

###### Entity Deletion:
if you call `book.OtherAuthors.DeleteAll()` the respective `author` entities are deleted from the `Authors` collection if they were linked entities.



## Relationships (Referenced)

referenced relationships require a bit of special handling. a **one-to-one** relationship is defined by using the `One<T>` class and **one-to-many** relationships are defined by using the `Many<TChild>` class. it is also a good idea to initialize the `Many` properties with the `Initialize()` method from the parent entity as shown below in order to avoid null-reference exceptions during runtime.

```csharp
    public class Book : Entity
    {
        public One<Author> MainAuthor { get; set; }
        public Many<Author> Authors { get; set; }
        
        public Book() => Authors = Authors.Initialize(this);
    }
```

#### One-to-one:

call the `ToReference()` method of the entity you want to store as a reference like so:

```csharp
    book.MainAuthor = author.ToReference();
    book.Save();
```

###### Reference Removal:
```csharp
    book.MainAuthor = null;
    book.Save();
```
the original `author` in the `Authors` collection is unaffected.

###### Entity Deletion:
If you delete an entity that is referenced as above by calling `author.Delete()` all references pointing to that entity are automatically deleted. as such, `book.MainAuthor.ToEntity()` will then be `null`.

#### One-to-many:
```charp
    book.Authors.Add(author);
```
there's no need to call `book.Save()` because references are automatically created and saved using special joining collections in the form of `Book_Author` in the database. you don't have to pay any attention to these special collections unless you rename your entities. for ex: if you rename the `Book` entity to `AwesomeBook` just rename the corresponding join collection from `Book_Author` to `AwesomeBook_Author` in order to get the references working again. 

###### Reference Removal:
```charp
    book.Authors.Remove(author);
```
the original `author` in the `Authors` collection is unaffected.

###### Entity Deletion:
If you delete an entity that is referenced as above by calling `author.Delete()` all references pointing to that entity are automatically deleted. as such, `book.Authors` will not have `author` as a child.

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
    var authorsQueryable = from a in author.Collection()
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

be mindful when changing the schema of your entities. the documents/entities stored in mongodb are always overwritten with the current schema/ shape of your entities. for example:

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

the data stored in `Price` will be lost if you do not manually handle the transfer of data from the old property to the new property upon saving.



## Examples

to see working examples please [click here](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples/Program.cs)

to see this library used in a real-world application, check the ASP.Net Core WebAPI project [click here](https://github.com/dj-nitehawk/KiwilinkCRM/tree/master/Kiwilink-API)



## Donations
if this library has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking the button below:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)