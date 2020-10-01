# Count entities
there are a couple of ways to get the count of entities stored in a collection.

### Count estimated total
```csharp
var count = await DB.CountEstimatedAsync<Author>();
```
you can get a fast estimate of total entities for a given entity type at the expense of accuracy. 
the above will give you a rough estimate of the total entities using collection meta-data.

### Count total entities
```csharp
var count = await DB.CountAsync<Author>();
```
the above will give you an accurate count of total entities by running an aggregation query.

### Count matches for an expression
```csharp
var count = await DB.CountAsync<Author>(a => a.Title == "The Power Of Now");
```
you can get the number of entities that matches a given expression/filter with the above.