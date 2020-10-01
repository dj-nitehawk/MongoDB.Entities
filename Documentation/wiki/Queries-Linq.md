# LINQ queries

see the mongodb c# driver [linq documentation](http://mongodb.github.io/mongo-csharp-driver/2.11/reference/driver/crud/linq/) to see which LINQ operations are available.
also see the c# driver [expressions documentation](http://mongodb.github.io/mongo-csharp-driver/2.11/reference/driver/expressions/) to see all supported expressions.

> [!tip] 
> don't forget to first import the mongodb linq extensions with **using MongoDB.Driver.Linq;**

## Query collections
```csharp
var author = await (from a in DB.Queryable<Author>()
                    where a.Name.Contains("Eckhart")
                    select a).FirstOrDefaultAsync();
```
### Collection shortcut
```csharp
var authors = from a in author.Queryable()
              select a;
```
this `.Queryable()` is an `IQueryable` for the whole collection of `Authors` which you can write queries against.

## Forward relationship access
every `Many<T>` property gives you access to an `IQueryable` of child entities.
```csharp
var authors = from a in book.Authors.ChildrenQueryable()
              select a;
```
this `.ChildrenQueryable()` is an already filtered `IQueryable` of child entities. For ex: the above `.ChildrenQueryable()` is limited to only the Authors of that particular `Book` entity. It does not give you access to all of the `Author` entities in the Authors collection.

## Reverse relationship access
for example, if you'd like to get all the books belonging to a genre, you can do it with the help of `.ParentsQueryable()` like so:
```csharp
var books = book.Genres
                .ParentsQueryable<Book>("GenreID");
```
you can also pass in an `IQueryable` of genres and get back an `IQueryable` of books like shown below:
```csharp
var query = genre.Queryable()
                 .Where(g => g.Name.Contains("Music"));

var books = book.Genres
                .ParentsQueryable<Book>(query);
```
it is basically a convenience method instead of having to do a manual join like the one shown below in order to access parents of one-to-many or many-to-many relationships.

## Relationship joins
`Many<T>.JoinQueryable()` gives you access to all the join records of that particular relationship. A join record has two properties `ParentID` and `ChildID` that you can use to gain access to parent Entities like so:
```csharp
var books = from j in book.Authors.JoinQueryable()
            join b in book.Queryable() on j.ParentID equals b.ID
            select b;
```

## Counting children
you can get how many entities are there in the opposite side of any relationship as shown below:
```csharp
var authorCount = await book.Authors.ChildrenCountAsync();
var bookCount = await author.Books.ChildrenCountAsync();
```