# Code Samples
---
### Initialize connection
```csharp
  await DB.InitAsync("bookshop","localhost");
```
### Persist an entity
```csharp
  var book = new Book { Title = "The Power Of Now" };
  await book.SaveAsync();
```
### Embed as document
```csharp
  var dickens = new Author { Name = "Charles Dickens" };
  book.Author = dickens.ToDocument();
  await book.SaveAsync();
```
### Update entity properties
```csharp
  await DB.Update<Book>()
          .Match(b => b.Title == "The Power Of Now")
          .Modify(b => b.Publisher, "New World Order")
          .Modify(b => b.ISBN, "SOMEISBNNUMBER")
          .ExecuteAsync();
```
### One-To-One relationship
```csharp
  var hemmingway = new Author { Name = "Ernest Hemmingway" };
  await hemmingway.SaveAsync();
  book.MainAuthor = hemmingway;
  await book.SaveAsync();
```
### One-To-Many relationship
```csharp
  var tolle = new Author { Name = "Eckhart Tolle" };
  await tolle.SaveAsync();
  await book.Authors.AddAsync(tolle);
```
### Many-To-Many relationship
```csharp
  var genre = new Genre { Name = "Self Help" };
  await genre.SaveAsync();
  await book.AllGenres.AddAsync(genre);
  await genre.AllBooks.AddAsync(book);
```        
### Queries
```csharp
  var author = await DB.Find<Author>().OneAsync("ID");

  var authors = await DB.Find<Author>().ManyAsync(a => a.Publisher == "Harper Collins");

  var eckhart = await DB.Queryable<Author>()
                        .Where(a => a.Name.Contains("Eckhart"))
                        .SingleOrDefaultAsync();

  var powerofnow = await genre.AllBooks.ChildrenQueryable()
                                       .Where(b => b.Title.Contains("Power"))
                                       .SingleOrDefaultAsync();

  var selfhelp = await book.AllGenres.ChildrenQueryable().FirstAsync();
```
### Delete
```csharp
  await book.MainAuthor.DeleteAsync();
  await book.AllAuthors.DeleteAllAsync();
  await book.DeleteAsync();
  await DB.DeleteAsync<Genre>("ID");
  await DB.DeleteAsync<Book>(b => b.Title == "The Power Of Now");
```
---

<div class="actions-container">
  <div><a href="Get-Started.md">Get Started</a></div>
</div>

---

# Tutorials
- [Beginners Guide](https://dev.to/djnitehawk/tutorial-mongodb-with-c-the-easy-way-1g68)
- [Fuzzy Text Search](https://dev.to/djnitehawk/mongodb-fuzzy-text-search-with-c-the-easy-way-3l8j)
- [GeoSpatial Search](https://dev.to/djnitehawk/tutorial-geospatial-search-in-mongodb-the-easy-way-kbd)
---
# More Examples
- [.Net core console project](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples)
- [Asp.net core web-api project](https://github.com/dj-nitehawk/MongoWebApiStarter)
- [A collection of gists](https://gist.github.com/dj-nitehawk)
- [Integration/unit test project](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)
- [Solutions to stackoverflow questions](https://stackoverflow.com/search?tab=newest&q=user%3a4368485%20%5bmongodb%5d)


