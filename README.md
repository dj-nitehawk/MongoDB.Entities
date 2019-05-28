[![](https://img.shields.io/nuget/v/MongoDB.Entities.svg)](#) [![](https://img.shields.io/nuget/dt/MongoDB.Entities.svg)](#)
# MongoDB.Entities
This library simplifies access to mongodb by abstracting away the C# mongodb driver and providing some additional features on top of it. The API is clean and intuitive resulting in less lines of code that is more readable/ human friendly than driver code.



### Advantages:
- Never have to deal with `ObjectIds` or `BsonDocuments`. 
- Everything is type safe. No magic strings needed.
- Model your entities in either **Document/NoSQL** stye or **Relational/SQL** style or a mix of both.
- Built-in automatic support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships.
- Data can be queried using either LINQ, lambda expressions or filters.
- Sorting, paging and projecting is convenient.
- Programatically define indexes.
- Full text search with text indexes.
- Supports `decimal` type without attributes.
- Supports both `ASP.Net Core` and `.Net Core` applications.



## Documentation
the API is described in the [wiki](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/1.-Getting-Started).



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
        var eckhart = DB.Collection<Author>()
                        .Where(a => a.Name.Contains("Eckhart"))
                        .SingleOrDefault();

        var powerofnow = genre.AllBooks.Collection()
                                       .Where(b => b.Title.Contains("Power"))
                                       .SingleOrDefault();

        var selfhelp = book.AllGenres.Collection().First();

    //Delete
        book.Delete();
        book.Authors.DeleteAll();
        DB.Delete<Genre>(genre.ID);
```



## Code Examples
.net core console project: [click here](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples)

e2e/unit test project: [click here](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)

asp.net core web-api project: [click here](https://github.com/dj-nitehawk/KiwilinkCRM/tree/master/Kiwilink-API)

solutions to stackoverflow questions: [click here](https://stackoverflow.com/search?tab=newest&q=user%3a4368485%20%5bmongodb%5d)



## Donations
if this library has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking the button below:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)