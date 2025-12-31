# Fluent aggregation pipelines

Most querying requirements can be catered to with the [_Find_](Queries-Find.md) & [_Queryable_](Queries-Linq.md) APIs. In case you need to build fluent aggregation pipelines, use the `Fluent` method for getting access to the `IAggregateFluent<T>` interface for a given entity type like so:

```csharp
var author = await db.Fluent<Author>()
                     .Match(a => a.Surname == "Stark" && a.Age > 10)
                     .SortByDescending(a => a.Age)
                     .SortBy(a => a.Name)
                     .Skip(1).Limit(1)
                     .Project(a => new { Test = a.Name })
                     .SingleOrDefaultAsync();
```

> [!tip]
> You'll have to add **using MongoDB.Driver;** import statement for the async extension methods such as **SingleOrDefaultAsync()** to work.

# GeoNear aggregation pipelines

In order to start a fluent aggregation pipeline with a `GeoNear` query, simply do the following:

```csharp
var query = db.GeoNear<Place>(
    NearCoordinates: new Coordinates2D(48.857908, 2.295243),
    DistanceField: x => x.DistanceMeters,
    MaxDistance: 20000);
```

The above code builds an aggregation pipeline that will find all the documents tagged with locations within 20Km from the eiffel tower in paris.

You can then add more pipeline stages to the above query in order to do further processing. You can specify all the supported options for `$geoNear` using the constructor above.

## Other fluent interfaces

There are also fluent counterparts of other methods such as:

```csharp
Many<T>.ChildrenFluent()         //pre-filtered children of the parent
Many<T>.ParentsFluent()          //access parents of a given child
Many<T>.JoinFluent()             //all records of the join collection
db.FluentTextSearch<T>()         //full text search
db.GeoNear<T>()                  //geospatial fluent pupeline
db.Fluent<T>().MatchExpression() //$expr queries
```