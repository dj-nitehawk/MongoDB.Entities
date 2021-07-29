# Paged search
paging in mongodb driver is typically achieved by running two separate db queries; one for the count and another for the actual entities. it can also be done via a `$facet` aggregation query, which is cumbersome to do using the driver. this library provides a convenient method for this exact use case via the `PagedSearch` builder.

## Example

```csharp
var res = await DB.PagedSearch<Book>()
                  .Match(b => b.AuthorName == "Eckhart Tolle")
                  .Sort(b => b.Title, Order.Ascending)
                  .PageSize(10)
                  .PageNumber(1)
                  .ExecuteAsync();

IReadOnlyList<Book> books = res.Results;
long totalMatchCount = res.TotalCount;
int totalPageCount = res.PageCount;                  
```

specify the search criteria with the `.Match()` method as you'd typically do. specify how to order the result set using the `.Sort()` method. specify the size of a single page using `.PageSize()` method. specify which page number to retrieve using `PageNumber()` method and finally issue the command using `ExecuteAsync()` to get the result of the facetted aggregation query.

the result is a value tuple consisting of the `Results`,`TotalCount`,`PageCount`.

> [!note] 
> if you do not specify a matching criteria, all entities will match. the default page size is 100 if not specified and the 1st page is always returned if you omit it.


## Project results to a different type
if you'd like to change the shape of the returned entity list, use the `PagedSearch<T, TProjection>` generic overload and add a `.Project()` method to the chain like so:
```csharp
var res = await DB.PagedSearch<Book, BookListing>()
                  .Sort(b => b.Title, Order.Ascending)
                  .Project(b => new BookListing
                  {
                      BookName = b.Title,
                      AuthorName = b.Author
                  })
                  .PageSize(25)
                  .PageNumber(1)
                  .ExecuteAsync();

IReadOnlyList<BookListing> listings = res.Results;
long totalMatchCount = res.TotalCount;
int totalPageCount = res.PageCount;                     
```

*when projecting to different types as above, you may encounter a deserialization error thrown by the driver saying it can't convert `ObjectId` values to `string` in which case simply add a `.ToString()` to the property being projected like so:*

```csharp
.Project(b => new BookListing
{
    ...
    BookID = b.ID.ToString(),
    ...
})
```

## Paging support for any fluent pipeline

you can add paged search to any [fluent pipeline](Queries-Pipelines.md). the difference is, instead of specifying the search criteria with `.Match()`, you start off by using the `.WithFluent()` method like so:

```csharp
var pipeline = DB.Fluent<Author>()
                 .Match(a => a.Name == "Author")
                 .SortBy(a => a.Name);

var res = await DB.PagedSearch<Author>()
                  .WithFluent(pipeline)
                  .Sort(a => a.Name, Order.Descending)
                  .PageNumber(1)
                  .PageSize(25)
                  .ExecuteAsync();
```

alternatively you can use the extension method on any fluent pipeline as well.
```csharp
var res = await pipeline.PagedSearch()
                        .Sort(a => a.Name, Order.Descending)
                        .PageSize(25)
                        .PageNumber(1)
                        .ExecuteAsync();
```

it's specially useful when you need to page children of a relationship like so:
```csharp
var res = await DB.Entity<Author>("AuthorID")
                  .Books
                  .ChildrenFluent()
                  .Match(b => b.Title.Contains("The"))
                  .PagedSearch()
                  .Sort(b => b.Title, Order.Ascending)
                  .PageNumber(1)
                  .PageSize(10)
                  .ExecuteAsync();
```