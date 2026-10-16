# Sequential number generation
Call `db.NextSequentialNumberAsync<T>()` to atomically increment and retrieve a counter for an entity's collection in that database. For example, use it to populate a separate business identifier:

```csharp
public class Person : Entity
{
    public string CustomID { get; set; } = "";
}
```
```csharp
var person = new Person();

var number = await db.NextSequentialNumberAsync<Person>();

person.CustomID = $"PID-{number:00000000}-X";

await db.SaveAsync(person);
```
For a new sequence, `CustomID` will be `PID-00000001-X`, followed by `PID-00000002-X`, `PID-00000003-X`, and so on. This does not change the entity's own `ID` property.

#### No entity instance required
The counter is accessed through `DB`; you do not need to construct a `Person`:

```csharp
var number = await db.NextSequentialNumberAsync<Person>();
```

#### Generation for any sequence name
There's also an overload for generating sequential numbers for any given sequence name like so:
```csharp
var number = await db.NextSequentialNumberAsync("SequenceName");
```

#### Considerations
Counters are scoped to the database and sequence name. The generic overload uses the entity's collection name, so types mapped to the same collection share a counter.

Calling this method issues a single db call in order to increment a counter document in the database and retrieve the number. 

Concurrent access won't result in duplicate numbers being generated but it would cause write locking and performance could suffer.

Multi db support and async methods with task cancellation support are also available.

Sequence increments run outside transactions, even when called through a transaction instance. An aborted transaction does not roll back an allocated number, so gaps are expected. Do not use these counters when gap-free numbering is required.
