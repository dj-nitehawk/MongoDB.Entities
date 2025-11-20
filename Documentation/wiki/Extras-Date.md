# The 'Date' Type
there's a special `Date` type you can use to store date/time values in mongodb instead of the regular **System.DateTime** type. the benefits of using it would be:
- preserves date/time precision
- can query using ticks
- can extend it by inheriting

## Examples
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
    var book = await db.Find<Book>()
                       .Match(b => b.PublishedOn.Ticks < DateTime.UtcNow.Ticks)
                       .ExecuteFirstAsync();

// query with 'DateTime'
    var book = await db.Find<Book>()
                       .Match(b => b.PublishedOn.DateTime < DateTime.UtcNow)
                       .ExecuteFirstAsync();

// set/change value with 'Ticks'
    date.Ticks = DateTime.UtcNow.Ticks;

// set/change value with 'DateTime'
    date.DateTime = DateTime.UtcNow;
```