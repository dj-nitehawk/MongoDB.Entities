# Code Samples
---

## Initialize connection

```csharp
  var db = await DB.InitAsync("bookshop");
```

## Persist an entity

```csharp
  var book = new Book { Title = "The Power Of Now" };
  await db.SaveAsync(book);
```

## Embed as document

```csharp
  var dickens = new Author { Name = "Charles Dickens" };
  book.Author = dickens.ToDocument();
  await db.SaveAsync(book);
```

## Update entity properties

```csharp
  await db.Update<Book>()
          .Match(b => b.Title == "The Power Of Now")
          .Modify(b => b.Publisher, "New World Order")
          .Modify(b => b.ISBN, "SOMEISBNNUMBER")
          .ExecuteAsync();
```

## One-To-One relationship

```csharp
  var hemmingway = new Author { Name = "Ernest Hemmingway" };
  await db.SaveAsync(hemmingway);
  book.MainAuthor = hemmingway;
  await db.SaveAsync(book);
```

## One-To-Many relationship

```csharp
  var tolle = new Author { Name = "Eckhart Tolle" };
  await db.SaveAsync(tolle);
  await book.Authors.AddAsync(tolle, db);
```

## Many-To-Many relationship

```csharp
  var genre = new Genre { Name = "Self Help" };
  await db.SaveAsync(genre);
  await book.AllGenres.AddAsync(genre, db);
  await genre.AllBooks.AddAsync(book, db);
```        

## Queries

```csharp
  var author = await db.Find<Author>().OneAsync("ID");

  var authors = await db.Find<Author>().ManyAsync(a => a.Publisher == "Harper Collins");

  var eckhart = await db.Queryable<Author>()
                        .Where(a => a.Name.Contains("Eckhart"))
                        .SingleOrDefaultAsync();
```

## Delete

```csharp
  await book.MainAuthor.DeleteAsync(db);
  await book.AllAuthors.DeleteAllAsync(db);
  await db.DeleteAsync(book);
  await db.DeleteAsync<Genre>("ID");
  await db.DeleteAsync<Book>(b => b.Title == "The Power Of Now");
```

---

<div style="display:flex;justify-content:left;gap:12px;margin:0;">
  <a href="Get-Started.md" style="display:inline-block;padding:10px 16px;background:#0078D4;color:#fff;border-radius:6px;text-decoration:none;font-weight:600;box-shadow:0 2px 4px rgba(0,0,0,0.1);">Get Started</a> 
  <a href="Performance-Benchmarks.md" style="display:inline-block;padding:10px 16px;background:#0A6ED1;color:#fff;border-radius:6px;text-decoration:none;font-weight:600;box-shadow:0 2px 4px rgba(0,0,0,0.1);">Benchmarks</a>
</div>

---

# Tutorials

- [Beginners Guide](https://dev.to/djnitehawk/tutorial-mongodb-with-c-the-easy-way-1g68)
- [Fuzzy Text Search](https://dev.to/djnitehawk/mongodb-fuzzy-text-search-with-c-the-easy-way-3l8j)
- [GeoSpatial Search](https://dev.to/djnitehawk/tutorial-geospatial-search-in-mongodb-the-easy-way-kbd)

---

# More Examples

- [Asp.net core web-api project](https://github.com/dj-nitehawk/MongoWebApiStarter)
- [Repository pattern project](https://github.com/dj-nitehawk/MongoDB-Entities-Repository-Pattern)
- [A collection of gists](https://gist.github.com/dj-nitehawk)
- [Integration/unit test project](https://github.com/dj-nitehawk/MongoDB.Entities/tree/master/Tests)
- [Solutions to stackoverflow questions](https://stackoverflow.com/search?tab=newest&q=user%3a4368485%20%5bmongodb%5d)