# String templates
the mongodb driver has it's limits and sometimes you need to compose queries either by hand or using a query editor/composer. to be able to run those queries and inject values from your C# code, you're required to compose `BsonDocuments` or do string concatenation which leads to an ugly, unreadable, magic string infested mess.

in order to combat this problem and also to couple your C# entity schema to raw queries, this library offers a templating system based on tag replacements.

take the following find query for example:
```java
db.Book.find(
  {
    Title : 'book_name',
    Price : book_price
  }
)
```
to couple this text query to your C# models and pass in the values for title and price, you simply surround the parts you want replaced with `<` and `>` in order to turn them in to replacement tags/markers like this:

```java
db.Book.find(
  {
    <Title> : '<book_name>',
    <Price> : <book_price>
  }
)
```

the templating system is based on a special class called `Template`. You simply instantiate a 'Template' object supplying the tagged/marked text query to the constructor. then you chain method calls on the Template object to replace each tag you've marked on the query like so:

```csharp
var query = new Template<Book>(@"
{
  <Title> : '<book_name>',
  <Price> : <book_price>
}")
.Path(b => b.Title)    
.Path(b => b.Price)
.Tag("book_name","The Power Of Now")
.Tag("book_price","10.95");

var result = await DB.Find<Book>()
                     .Match(query)
                     .ExecuteAsync();
```

the resulting query sent to mongodb is this:
```java
db.Book.find(
  {
    Title : 'The Power Of Now',
    Price : 10.95
  }
)
```

the `.Tag()` method simply replaces matching tags on the text query with the supplied value. you don't have to use the `<` and `>` characters while using the `.Tag()` method. infact, avoid it as the tags won't match if you use them.

the `.Path()` method is one of many offered by the `Prop` class you can use to get the full 'dotted' path of a property by supplying a lambda/member expression. please see the documentation of the 'Prop' class [here](Extras-Prop.md) for the other methods available.

notice, that most of these 'Prop' methods only require a single parameter. whatever member expression you supply to them gets converted to a property/field path like this:

> expression: x => x.Authors[0].Books[0].Title 

> resulting path: Authors.Books.Title

if your text query has a tag `<Authors.Books.Title>` it will get replaced by the resulting path from the 'Prop' class method.

the template system will throw an exception in the event of the following 3 scenarios.

1. the input query/text has no tags marked using `<` and `>` characters.
2. the input query has tags that you forget to specify replacements for.
3. you have specified replacements that doesn't have a matching tag in the query.

this kind of runtime errors are preferable than your code failing silently because the queries didn't produce any results or produced the wrong results.

# Examples

### Aggregation pipeline
```csharp
var pipeline = new Template<Book>(@"
[
    {
      $match: { <Title>: '<book_name>' }
    },
    {
      $sort: { <Price>: 1 }
    },
    {
      $group: {
        _id: '$<AuthorId>',
        product: { $first: '$$ROOT' }
      }
    },
    {
      $replaceWith: '$product'
    }
]")
.Path(b => b.Title)
.Path(b => b.Price)
.Path(b => b.AuthorId)
.Tag("book_name", "MongoDB Templates");

var book = await DB.PipelineSingleAsync(pipeline);
```

### Aggregation pipeline with different result type

```csharp
var pipeline = new Template<Book, Author>(@"
[
    {
        $match: { _id: <book_id> }
    },
    {
        $lookup: 
        {
            from: '<author_collection>',
            localField: '<AuthorID>',
            foreignField: '_id',
            as: 'authors'
        }
    },
    {
        $replaceWith: { $arrayElemAt: ['$authors', 0] }
    },
    {
        $set: { <Age> : 34 }
    }
]")
.Tag("book_id", "ObjectId('5e572df44467000021005692')")
.Tag("author_collection", DB.Entity<Author>().CollectionName())
.Path(b => b.AuthorID)
.PathOfResult(a => a.Age);

var authors = await DB.PipelineAsync(pipeline);
```

### Find with match expression

```csharp
var query = new Template<Author>(@"
{
  $and: [
    { $gt: [ '$<Age>', <author_age> ] },
    { $eq: [ '$<Surname>', '<author_surname>' ] }
  ]
}")
.Path(a => a.Age)
.Path(a => a.Surname)
.Tag("author_age", "54")
.Tag("author_surname", "Tolle");

var authors = await DB.Find<Author>()
                      .MatchExpression(query)
                      .ExecuteAsync();
```

### Update with aggregation pipeline
```csharp
var pipeline = new Template<Author>(@"
[
  { $set: { <FullName>: { $concat: ['$<Name>',' ','$<Surname>'] } } },
  { $unset: '<Age>'}
]")             
.Path(a => a.FullName)
.Path(a => a.Name)
.Path(a => a.Surname)
.Path(a => a.Age);

await DB.Update<Author>()
        .Match(a => a.ID == "xxxxx")
        .WithPipeline(pipeline)
        .ExecutePipelineAsync();
```

### Update with array filters
```csharp
var filters = new Template<Author>(@"
[
  { '<a.Age>': { $gte: <age> } },
  { '<b.Name>': 'Echkart Tolle' }
]")
.Elements(0, author => author.Age)
.Elements(1, author => author.Name);
.Tag("age", "55")        

var update = new Template<Book>(@"
{ $set: { 
    '<Authors.$[a].Age>': <age>,
    '<Authors.$[b].Name>': '<name>'
  } 
}")
.PosFiltered(book => book.Authors[0].Age)
.PosFiltered(book => book.Authors[1].Name)
.Tag("age", "55")
.Tag("name", "Updated Name");

await DB.Update<Book>()
        .Match(book => book.ID == "xxxxxxxx")
        .WithArrayFilters(filters)
        .Modify(update)
        .ExecuteAsync();
```