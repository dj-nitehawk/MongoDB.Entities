# Code Samples
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
  await DB.DeleteAsync<Genre>(genre.ID);
```
# More Examples
- [.net core console project](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Examples)
- [asp.net core web-api project](https://github.com/dj-nitehawk/MongoWebApiStarter)
- [a collection of gists](https://gist.github.com/dj-nitehawk)
- [integration/unit test project](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)
- [solutions to stackoverflow questions](https://stackoverflow.com/search?tab=newest&q=user%3a4368485%20%5bmongodb%5d)


