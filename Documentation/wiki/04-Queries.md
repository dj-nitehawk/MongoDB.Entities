data can be queried using LINQ, lambda expressions, filters or aggregation pipelines as described below.

### LINQ

see the mongodb c# driver [linq documentation](http://mongodb.github.io/mongo-csharp-driver/2.9/reference/driver/crud/linq/) to see which LINQ operations are available.
also see the c# driver [expressions documentation](http://mongodb.github.io/mongo-csharp-driver/2.9/reference/driver/expressions/) to see all supported expressions.

> don't forget to first import the mongodb linq extensions with `using MongoDB.Driver.Linq;`

#### Entity collections:
```csharp
    var author = await (from a in DB.Queryable<Author>()
                        where a.Name.Contains("Eckhart")
                        select a).FirstOrDefaultAsync();
```
#### Shortcut for entity collections:
```csharp
    var authors = from a in author.Queryable()
                  select a;
```
this `.Queryable()` is an `IQueryable` for the whole collection of `Authors` which you can write queries against.

#### Forward Relationship Access:
every `Many<T>` property gives you access to an `IQueryable` of child entities.
```csharp
    var authors = from a in book.Authors.ChildrenQueryable()
                  select a;
```
this `.ChildrenQueryable()` is an already filtered `IQueryable` of child entities. For ex: the above `.ChildrenQueryable()` is limited to only the Authors of that particular `Book` entity. It does not give you access to all of the `Author` entities in the Authors collection.

#### Reverse Relationship Access:
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

#### Relationship Joins:
`Many<T>.JoinQueryable()` gives you access to all the join records of that particular relationship. A join record has two properties `ParentID` and `ChildID` that you can use to gain access to parent Entities like so:
```csharp
    var books = from j in book.Authors.JoinQueryable()
                join b in book.Queryable() on j.ParentID equals b.ID
                select b;
```
#### Counting Children
you can get how many entities are there in the opposite side of any relationship as shown below:
```csharp
    var authorCount = await book.Authors.ChildrenCountAsync();
    var bookCount = await author.Books.ChildrenCountAsync();
```

### Find Queries
several overloads are available for finding entities as shown below.
#### Find One By ID
```csharp
    var author = await DB.Find<Author>().OneAsync("ID");
```
#### Find Many By Lambda
```csharp
    var authors = await DB.Find<Author>().ManyAsync(a => a.Publisher == "Harper Collins");
```
#### Find Many By Filter
```csharp
    var authors = await DB.Find<Author>()
                          .ManyAsync(f=> f.Eq(a=>a.Surname,"Stark") & f.Gt(a=>a.Age,35));
```
> all the filters in the official driver are available for use as shown above.

#### Find By 2D Coordinates
```csharp
    var cafes = await DB.Find<Cafe>()
                        .Match(c => c.Location, new Coordinates2D(48.857908, 2.295243), 1000)
                        .ExecuteAsync()
```
> see [this tutorial](https://dev.to/djnitehawk/tutorial-geospatial-search-in-mongodb-the-easy-way-kbd) for a detailed walkthrough.
#### Find By Aggregation Expression ($expr)
```csharp
    var authors = await DB.Find<Author>()
                          .MatchExpression("{$gt:['$TotalSales','$SalesGoal']}")
                          .ExecuteAsync();
```
> aggregation [expressions](https://docs.mongodb.com/manual/reference/operator/query/expr/) lets you refer to properties from the same entity using the $ notation as shown above.

#### Advanced Find With Sorting, Paging and Projection
```csharp
    var authors = await DB.Find<Author>()
                          .Match(a => a.Age > 30)
                          .Sort(a => a.Age, Order.Descending)
                          .Sort(a => a.Name, Order.Ascending)
                          .Skip(1).Limit(1)
                          .Project(a => new Author { Name = a.Name })
                          .ExecuteAsync();
```
the search criteria is specified using `.Match()` which takes either an **ID**, **lambda expression**, **filter expression**, **geospatial**, or **full/fuzzy text search query**.

sorting is specified using `.Sort()` which takes in a lambda for the property to sort by and in which order. `.Sort()` can be used multiple times in order to specify multiple sorting stages. when doing text queries, you can sort the results by mongodb's 'meta text score' by using the `.SortByTextScore()` method.

how many items to skip and take are specified using `.Skip()` and `.Limit()`

to avoid the complete entity being returned, you can use `.Project()` with a lambda expression to get back only the properties you need. it is also possible to use projection builder methods like so:

```csharp
    .Project(p => p.Include("Name").Exclude("Surname"))
```
> to be able to chain projection builder methods like above, please add the import statement `using MongoDB.Driver;` to your class.

it is also possible to specify an exclusion projection with a `new` expression like so:

```csharp
    var res = await DB.Find<Author>()
                      .Match(a => a.ID == "xxxxxxxxxxx")
                      .ProjectExcluding(a => new { a.Age, a.Name })
                      .ExecuteSingleAsync();
```

doing so will return an Author entity with all the properties populated except for the Age and Name properties.

you can also project to a different output type using the generic overload `DB.Find<TEntity,TProjection>` generic overload.

an `.Execute*()` method is called finally to get back the result of the find command. you can also get a cursor back instead of materialized results by calling `.ExecuteCursorAsync()` at the end.

> there are 3 variations of `Execute*()` you can use. `ExecuteAsync()` which will return a list of matched entities. `ExecuteSingleAsync()` which will return only 1 matched entity and will throw an exception if more than 1 entity is matched. `ExecuteFirstAsync()` which will return the first matched entity. all variations will return a `null/default` value if nothing was matched.

### Fluent Aggregation Pipelines
99% of querying requirements can be catered to with the above APIs. in case you need to build fluent aggregation pipelines, use the `Fluent` method for getting access to the `IAggregateFluent<T>` interface for a given entity type like so:
```csharp
    var author = await DB.Fluent<Author>()
                         .Match(a => a.Surname == "Stark" && a.Age > 10)
                         .SortByDescending(a => a.Age)
                         .ThenByAscending(a => a.Name)
                         .Skip(1).Limit(1)
                         .Project(a => new { Test = a.Name })
                         .SingleOrDefaultAsync();
```
> you'll have to add `using MongoDB.Driver;` import statement for the async extension methods such as `SingleOrDefaultAsync()` to work.

there are also fluent counterparts of other methods such as:
```csharp
    Many<T>.ChildrenFluent() //pre-filtered children of the parent
    Many<T>.ParentsFluent() //access parents of a given child
    Many<T>.JoinFluent() //all records of the join collection
    Transaction.Fluent<T>() //transactional variation of DB.Fluent<T>()
    DB.FluentTextSearch<T>() //full text search    
    Transaction.FluentTextSearch<T>() //transactional full text search
    DB.Fluent<T>().MatchExpression() //$expr queries
    author.Fluent() //shortcut for DB.Fluent<Author>()
```
#### GeoNear Aggregation Pipelines
in order to start a fluent aggregation pipeline with a `GeoNear` query, simply do the following:
```csharp
var query = DB.FluentGeoNear<Place>(
               NearCoordinates: new Coordinates2D(48.857908, 2.295243),
               DistanceField: x => x.DistanceMeters,
               MaxDistance: 20000);
```
the above code builds an aggregation pipeline that will find all the documents tagged with locations within 20Km from the eiffel tower in paris. 

you can then add more pipeline stages to the above query in order to do further processing. you can specify all the supported options for `$geoNear` using the constructor above.

### [Next Page >>](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/05.-Indexes)