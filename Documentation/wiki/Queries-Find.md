# Find queries
several overloads are available for finding entities as shown below.
### Find one by ID
```csharp
var author = await DB.Find<Author>().OneAsync("ID");
```
### Find many by lambda
```csharp
var authors = await DB.Find<Author>().ManyAsync(a => a.Publisher == "Harper Collins");
```
### Find many by filter
```csharp
var authors = await DB.Find<Author>()
                      .ManyAsync(f=> f.Eq(a=>a.Surname,"Stark") & f.Gt(a=>a.Age,35));
```
> [!tip]
> all the [_filter definition builder_](https://mongodb.github.io/mongo-csharp-driver/2.11/apidocs/html/Methods_T_MongoDB_Driver_FilterDefinitionBuilder_1.htm) methods of the official driver are available for use as shown above.
### Find by 2D coordinates
```csharp
var cafes = await DB.Find<Cafe>()
                    .Match(c => c.Location, new Coordinates2D(48.857908, 2.295243), 1000)
                    .ExecuteAsync()
```
> see [_this tutorial_](https://dev.to/djnitehawk/tutorial-geospatial-search-in-mongodb-the-easy-way-kbd) for a detailed walkthrough.
## Find by aggregation expression ($expr)
```csharp
var authors = await DB.Find<Author>()
                      .MatchExpression("{$gt:['$TotalSales','$SalesGoal']}")
                      .ExecuteAsync();
```
> [!tip]
> aggregation [_expressions_](https://docs.mongodb.com/manual/reference/operator/query/expr/) lets you refer to properties of the same entity using the $ notation as well as enable you to use aggregation framework operators in find queries.

# Advanced find
## Sorting, paging and projecting
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
### Projections
to avoid the complete entity being returned, you can use `.Project()` with a lambda expression to get back only the properties you need as shown above. it is also possible to use projection builder methods like so:
```csharp
.Project(p => p.Include("Name").Exclude("Surname"))
```
> [!tip]
> to be able to chain projection builder methods like above, please add the import statement **using MongoDB.Driver;** to your class.
#### Projection with exclusions
it is also possible to specify an exclusion projection with a `new` expression like so:
```csharp
var res = await DB.Find<Author>()
                  .Match(a => a.ID == "xxxxxxxxxxx")
                  .ProjectExcluding(a => new { a.Age, a.Name })
                  .ExecuteSingleAsync();
```
doing so will return an Author entity with all the properties populated except for the Age and Name properties.

#### Project to a different type
in order to project to a different result type than the input entity type, simply use the generic overload like so:
```csharp
var name = await DB.Find<Author,string>()
                   .Match(a => a.ID == "xxxxxxxxxxx")
                   .Project(a => a.FirstName + " " + a.LastName)
                   .ExecuteSingleAsync();
```

## Execute
no command is sent over the wire to mongodb until you call one of the following `Execute*()` methods:

**ExecuteCursorAsync():** gets a cursor you can iterate over instead of a list of entities.

**ExecuteAsync():** gets a list of matched entities or default value if nothing matched.

**ExecuteSingleAsync():** gets only 1 matched entity and will throw an exception if more than 1 entity is matched, or default value if nothing matched.

**ExecuteFirstAsync():** gets the first of the matched entities, or default value if nothing matched.

**ExecuteAnyAsync():** gets a boolean indicating whether there were any matches.