[![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/1?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=1)



# MongoDB.Entities
This library simplifies access to mongodb by abstracting away the C# mongodb driver and providing some additional features on top of it. The API is clean and intuitive resulting in less lines of code that is more readable/ human friendly than driver code.



### Advantages:
- Never have to deal with `ObjectIds`, `BsonDocuments` & magic strings. 
- Built-in support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships.
- Query data using LINQ, lambda expressions, filters and fluent aggregations.
- Sorting, paging and projecting is made convenient.
- Simple data migration framework similar to EntityFramework.
- Programatically define indexes.
- Full text search (including fuzzy matching) with text indexes.
- Multi-document transaction support.
- Easy bulk operations.
- Update with aggregation pipeline stages & array filters.
- Easy GeoSpatial search. 
- Stream files in chunks to and from mongodb (GridFS alternative).
- Multiple database support.
- Project types supported: .Net Standard 2.0 (.Net Core 2.0 or higher & .Net Framework 4.6.1 or higher)



## Documentation
the easy to use API is described in the [wiki pages](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/01.-Getting-Started).



## Code Sample
```csharp
    //Initialize database connection
        new DB("bookshop");

    //Create and persist an entity
        var book = new Book { Title = "The Power Of Now" };
        book.Save();
 
    //Embed as document
        var dickens = new Author { Name = "Charles Dickens" };
        book.RelatedAuthor = dickens.ToDocument();
        dickens.Save();
    
    //One-To-One relationship
        var hemmingway = new Author { Name = "Ernest Hemmingway" };
        hemmingway.Save();
        book.MainAuthor = hemmingway.ToReference();

    //One-To-Many relationship
        var tolle = new Author { Name = "Eckhart Tolle" };
        tolle.Save();
        book.Authors.Add(tolle);

    //Many-To-Many relationship
        var genre = new Genre { Name = "Self Help" };
        genre.Save();
        genre.AllBooks.Add(book);

    //Queries
        var author = DB.Find<Author>().One("ID");

        var authors = DB.Find<Author>().Many(a => a.Publisher == "Harper Collins");

        var eckhart = DB.Queryable<Author>()
                        .Where(a => a.Name.Contains("Eckhart"))
                        .SingleOrDefault();

        var powerofnow = genre.AllBooks.ChildrenQueryable()
                                       .Where(b => b.Title.Contains("Power"))
                                       .SingleOrDefault();

        var genre = book.AllGenres.ChildrenQueryable().First();

    //Delete
        book.Delete();
        book.Authors.DeleteAll();
        DB.Delete<Genre>(genre.ID);
```



## Code Examples
.net core console project: [click here](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples)

asp.net core web-api project: [click here](https://github.com/dj-nitehawk/MongoWebApiStarter)

integration/unit test project: [click here](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)

solutions to stackoverflow questions: [click here](https://stackoverflow.com/search?tab=newest&q=user%3a4368485%20%5bmongodb%5d)



## Donations
if this library has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking the button below:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)