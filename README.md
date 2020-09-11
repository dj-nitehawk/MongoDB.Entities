[![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/1?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=1) [![license](https://img.shields.io/github/license/dj-nitehawk/MongoDB.Entities?color=blue&label=license&logo=Github&style=flat-square)](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/README.md)



# MongoDB.Entities
This library simplifies access to mongodb by abstracting away the C# mongodb driver and providing some additional features on top of it. The API is clean and intuitive resulting in less lines of code that is more readable/ human friendly than driver code.



### Advantages:
- Don't have to deal with `ObjectIds`, `BsonDocuments` & magic strings unless you want to.
- Built-in support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships.
- Async only API for scalable application development.
- Query data using LINQ, lambda expressions, filters and aggregation pipelines.
- Sorting, paging and projecting is made convenient.
- Simple data migration framework similar to EntityFramework.
- Programatically define indexes.
- Full text search (including fuzzy matching) with text indexes.
- Multi-document transaction support.
- Multiple database support.
- Easy bulk operations.
- Easy Change-stream support.
- GeoSpatial search.
- Stream files in chunks to and from mongodb (GridFS alternative).
- Project types supported: .Net Standard 2.0 (.Net Core 2.0 & .Net Framework 4.6.1 or higher)



## Documentation
- [API Reference](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/01.-Getting-Started)
- [Getting Started Tutorial](https://dev.to/djnitehawk/tutorial-mongodb-with-c-the-easy-way-1g68)
- [Fuzzy Text Search Tutorial](https://dev.to/djnitehawk/mongodb-fuzzy-text-search-with-c-the-easy-way-3l8j)
- [GeoSpatial Search Tutorial](https://dev.to/djnitehawk/tutorial-geospatial-search-in-mongodb-the-easy-way-kbd)


## Code Sample
```csharp
    //Initialize database connection
        await DB.InitAsync("bookshop","localhost");

    //Create and persist an entity
        var book = new Book { Title = "The Power Of Now" };
        await book.SaveAsync();
 
    //Embed as document
        var dickens = new Author { Name = "Charles Dickens" };
        book.Author = dickens.ToDocument();
        await book.SaveAsync();
    
    //One-To-One relationship
        var hemmingway = new Author { Name = "Ernest Hemmingway" };
        await hemmingway.SaveAsync();
        book.MainAuthor = hemmingway;
        await book.SaveAsync();

    //One-To-Many relationship
        var tolle = new Author { Name = "Eckhart Tolle" };
        await tolle.SaveAsync();
        await book.Authors.AddAsync(tolle);

    //Many-To-Many relationship
        var genre = new Genre { Name = "Self Help" };
        await genre.SaveAsync();
        await book.AllGenres.AddAsync(genre);
        await genre.AllBooks.AddAsync(book);

    //Queries
        var author = await DB.Find<Author>().OneAsync("ID");

        var authors = await DB.Find<Author>().ManyAsync(a => a.Publisher == "Harper Collins");

        var eckhart = await DB.Queryable<Author>()
                              .Where(a => a.Name.Contains("Eckhart"))
                               SingleOrDefaultAsync();

        var powerofnow = await genre.AllBooks.ChildrenQueryable()
                                             .Where(b => b.Title.Contains("Power"))
                                             .SingleOrDefaultAsync();

        var selfhelp = await book.AllGenres.ChildrenQueryable().FirstAsync();

    //Delete
        await book.MainAuthor.DeleteAsync();
        await book.AllAuthors.DeleteAllAsync();
        await book.DeleteAsync();
        await DB.DeleteAsync<Genre>(genre.ID);
```



## Code Examples
- [.net core console project](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples)
- [asp.net core web-api project](https://github.com/dj-nitehawk/MongoWebApiStarter)
- [a collection of gists](https://gist.github.com/dj-nitehawk)
- [integration/unit test project](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)
- [solutions to stackoverflow questions](https://stackoverflow.com/search?tab=newest&q=user%3a4368485%20%5bmongodb%5d)



## Donations
if this library has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking the button below:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)
