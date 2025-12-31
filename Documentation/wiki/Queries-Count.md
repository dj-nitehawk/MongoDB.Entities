# Count entities

There are a couple of ways to get the count of entities stored in a collection.

## Count estimated total

```csharp
var count = await db.CountEstimatedAsync<Author>();
```

You can get a fast estimate of total entities for a given entity type at the expense of accuracy.
The above will give you a rough estimate of the total entities using collection meta-data.

## Count total entities

```csharp
var count = await db.CountAsync<Author>();
```

The above will give you an accurate count of total entities by running an aggregation query.

## Count matches with an expression

```csharp
var count = await db.CountAsync<Author>(a => a.Title == "The Power Of Now");
```

## Count matches with a filter builder function

```csharp
var count = await db.CountAsync<Author>(b => b.Eq(a => a.Name, "Eckhart Tolle"));
```

## Count matches with a filter definition

```csharp
var filter = DB.Filter<Author>()
               .Eq(a => a.Name, "Eckhart Tolle");

var count = await db.CountAsync(filter);
```

## Counting children of a relationship

You can get how many entities are there in the opposite side of any relationship as shown below:

```csharp
var authorCount = await book.Authors.ChildrenCountAsync();
var bookCount = await author.Books.ChildrenCountAsync();
```