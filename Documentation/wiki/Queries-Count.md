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

### Count matches with an expression
```csharp
var count = await DB.CountAsync<Author>(a => a.Title == "The Power Of Now");
```

### Count matches with a filter builder function
```csharp
var count = await DB.CountAsync<Author>(b => b.Eq(a => a.Name, "Eckhart Tolle"));
```

### Count matches with a filter definition
```csharp
var filter = DB.Filter<Author>()
               .Eq(a => a.Name, "Eckhart Tolle");

var count = await DB.CountAsync(filter);
```
