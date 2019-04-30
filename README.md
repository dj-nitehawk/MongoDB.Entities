[![](https://img.shields.io/nuget/v/MongoDB.Entities.svg)](#) [![](https://img.shields.io/nuget/dt/MongoDB.Entities.svg)](#)
# MongoDB.Entities
The goal of this library is to simplify access to mongodb by wrapping up the official C# mongodb driver and providing some additional features on top of it. You get all the power of the official driver and then some. The API is clean and intuitive resulting in less lines of code that is more readable/ human friendly than driver code.


You never have to deal with `ObjectIds` or `BsonDocuments`. Everything will be type safe. You can get the best of both worlds by modelling your entities in either **Document/NoSQL** stye or **Relational/SQL** style or a mix of both. 


There is built-in automatic support for `One-To-One`, `One-To-Many` and `Many-To-Many` relationships. 


Data can be queried using either LINQ or lambda expressions.


Supports both `ASP.Net Core` and `.Net Core` applications.



## Code Sample
```csharp
    //Initialize database connection
        new DB("bookshop");

    //Create and persist an entity
        var book = new Book { Title = "The Power Of Now" };
        book.Save();

    //One-To-Many Relationship
        var author = new Author { Name = "Eckhart Tolle" };
        author.Save();
        book.Authors.Add(author);

    //Many-To-Many Relationship
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
        genre.Delete();
        book.Delete();
        author.Delete();
```



## Wiki/Getting Started
in order to get started using the library please see the [wiki pages](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/1.-Getting-Started).



## Example Projects
.net core console project: [click here](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples)

e2e/unit test project: [click here](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)

asp.net core web-api project: [click here](https://github.com/dj-nitehawk/KiwilinkCRM/tree/master/Kiwilink-API)



## Donations
if this library has made your life easier and you'd like to express your gratitude, you can donate a couple of bucks via paypal by clicking the button below:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9LM2APQXVA9VE)