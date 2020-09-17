# Fluent aggregation pipelines
99% of querying requirements can be catered to with the [_Find_](Queries-Find.md) & [_Queryable_](Queries-Linq.md) APIs. in case you need to build fluent aggregation pipelines, use the `Fluent` method for getting access to the `IAggregateFluent<T>` interface for a given entity type like so:
```csharp
var author = await DB.Fluent<Author>()
                     .Match(a => a.Surname == "Stark" && a.Age > 10)
                     .SortByDescending(a => a.Age)
                     .ThenByAscending(a => a.Name)
                     .Skip(1).Limit(1)
                     .Project(a => new { Test = a.Name })
                     .SingleOrDefaultAsync();
```
> [!tip]
> you'll have to add **using MongoDB.Driver;** import statement for the async extension methods such as **SingleOrDefaultAsync()** to work.

# GeoNear aggregation pipelines
in order to start a fluent aggregation pipeline with a `GeoNear` query, simply do the following:
```csharp
var query = DB.FluentGeoNear<Place>(
               NearCoordinates: new Coordinates2D(48.857908, 2.295243),
               DistanceField: x => x.DistanceMeters,
               MaxDistance: 20000);
```
the above code builds an aggregation pipeline that will find all the documents tagged with locations within 20Km from the eiffel tower in paris. 

you can then add more pipeline stages to the above query in order to do further processing. you can specify all the supported options for `$geoNear` using the constructor above.

# Other fluent interfaces
there are also fluent counterparts of other methods such as:
```csharp
Many<T>.ChildrenFluent() //pre-filtered children of the parent
Many<T>.ParentsFluent() //access parents of a given child
Many<T>.JoinFluent() //all records of the join collection
Transaction.Fluent<T>() //transactional variation of DB.Fluent<T>()
DB.FluentTextSearch<T>() //full text search
DB.FluentGeoNear<T>() //geospatial fluent pupeline
Transaction.FluentTextSearch<T>() //transactional full text search
DB.Fluent<T>().MatchExpression() //$expr queries
author.Fluent() //shortcut for DB.Fluent<Author>()
```