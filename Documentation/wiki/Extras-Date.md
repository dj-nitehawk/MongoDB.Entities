# The 'Date' Type
There's a special `Date` type you can use to store date/time values in mongodb instead of the regular **System.DateTime** type. The benefits of using it would be:
- Preserves date/time precision
- Can query using ticks
- Can extend it by inheriting

## Examples
```csharp
public class Book : Entity
{
    public Date PublishedOn { get; set; } = new();
}
```

```csharp
// Save an entity. DateTime is not implicitly converted to Date.
var book = new Book { PublishedOn = new Date(DateTime.UtcNow) };
await db.SaveAsync(book);
// DateTime.UtcNow.ToDate() is another way to create a Date.

// Query with ticks.
var byTicks = await db.Find<Book>()
                      .Match(b => b.PublishedOn.Ticks < DateTime.UtcNow.Ticks)
                      .ExecuteFirstAsync();

// Query with DateTime.
var byDate = await db.Find<Book>()
                     .Match(b => b.PublishedOn.DateTime < DateTime.UtcNow)
                     .ExecuteFirstAsync();

// Either setter keeps the Date object's two representations in sync.
book.PublishedOn.Ticks = DateTime.UtcNow.Ticks;
book.PublishedOn.DateTime = DateTime.UtcNow;

// Persist changes explicitly.
await db.SaveAsync(book);
```
