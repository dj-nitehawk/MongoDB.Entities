### The 'Date' Type
there's a special `Date` type you can use to store date/time values in mongodb instead of the regular **System.DateTime** type. the benefits of using it would be:
- preserves date/time precision
- can query using ticks
- can extend it by inheriting
- implicitly assignable to and from _System.DateTime_
#### Examples:
```csharp
// define the entity
    public class Book : Entity
    {
        public Date PublishedOn { get; set; }
    }

// save the entity
    new Book
    {
        PublishedOn = DateTime.UtcNow
    }
    .Save();

// query with 'Ticks'
    var book = await DB.Find<Book>()
                       .Match(b => b.PublishedOn.Ticks < DateTime.UtcNow.Ticks)
                       .ExecuteFirstAsync();

// query with 'DateTime'
    var book = await DB.Find<Book>()
                       .Match(b => b.PublishedOn.DateTime < DateTime.UtcNow)
                       .ExecuteFirstAsync();

// assign to 'DateTime' from 'Date'
    DateTime dt = book.PublishedOn;

// assign from 'DateTime' to 'Date'
    Date date = dt;

// set/change value with 'Ticks'
    date.Ticks = DateTime.UtcNow.Ticks;

// set/change value with 'DateTime'
    date.DateTime = DateTime.UtcNow;
```

### The 'Prop' Class
this static class has several handy methods for getting string property paths from lambda expressions. which can help to eliminate magic strings from your code during advanced scenarios.

#### Prop.Path()
returns the full dotted path for a given member expression.
> Authors[0].Books[0].Title > Authors.Books.Title
```csharp
    var path = Prop.Path<Book>(b => b.Authors[0].Books[0].Title);
```

#### Prop.Property()
returns the last property name for a given member expression.
> Authors[0].Books[0].Title > Title
```csharp
    var propName = Prop.Property<Book>(b => b.Authors[0].Books[0].Title);
```

#### Prop.Collection()
returns the collection/entity name for a given entity type.
```csharp
    var collectionName = Prop.Collection<Book>();
```

#### Prop.PosAll()
returns a path with the all positional operator $[] for a given expression.
> Authors[0].Name > Authors.$[].Name
```csharp
    var path = Prop.PosAll<Book>(b => b.Authors[0].Name);
```

#### Prop.PosFirst()
returns a path with the first positional operator $ for a given expression.
> Authors[0].Name > Authors.$.Name
```csharp
    var path = Prop.PosFirst<Book>(b => b.Authors[0].Name);
```

#### Prop.PosFiltered()
returns a path with filtered positional identifiers $[x] for a given expression.
> Authors[0].Name > Authors.$[a].Name

> Authors[1].Age > Authors.$[b].Age

> Authors[2].Books[3].Title > Authors.$[c].Books.$[d].Title

index positions start from [0] which is converted to $[a] and so on.
```csharp
    var path = Prop.PosFiltered<Book>(b => b.Authors[2].Books[3].Title);
```

#### Prop.Elements(index, expression)
returns a path with the filtered positional identifier prepended to the property path.
> (0, x => x.Rating) > a.Rating

> (1, x => x.Rating) > b.Rating

index positions start from '0' which is converted to 'a' and so on.
```csharp
    var res = Prop.Elements<Book>(0, x => x.Rating);
```

#### Prop.Elements()
returns a path without any filtered positional identifier prepended to it.
> b => b.Tags > Tags
```csharp
    var path = Prop.Elements<Book>(b => b.Tags);
```

### Sequential Number Generation
we can get mongodb to return a sequentially incrementing number everytime the method `.NextSequentialNumber()` on an Entity is called. it can be useful when you need to generate custom IDs like in the example below:

```csharp
    public class Person : Entity
    {
        public string CustomID { get; set; }
    }
```
```csharp
    var person = new Person();

    var number = await person.NextSequentialNumberAsync();

    person.CustomID = $"PID-{number:00000000}-X";

    person.Save();
```
the value of `CustomID` would be `PID-0000001-X`. the next Person entities you create/save would have `PID-0000002-X`, `PID-0000003-X`, `PID-0000004-X` and so on.

#### Alternative Static Method
if you don't have an instance of an Entity you can simply call the static method on the `DB` class like so:

```csharp
    var number = await DB.NextSequentialNumberAsync<Person>();
```

#### Generation For Any Sequence Name
there's also an overload for generating sequential numbers for any given sequence name like so:
```csharp
    var number = await DB.NextSequentialNumberAsync("SequenceName");
```

#### Considerations
keep in mind that there will be a separate sequence of numbers for each Entity type. 

calling this method issues a single db call in order to increment a counter document in the database and retrieve the number. 

concurrent access won't result in duplicate numbers being generated but it would cause write locking and performance could suffer.

multi db support and async methods with task cancellation support are also available.

there is no transaction support in order to avoid number generation unpredictability. however, you can call this method from within a transaction without any trouble.
