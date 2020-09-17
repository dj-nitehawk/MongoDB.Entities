
# Sequential number generation
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

#### Alternative static method
if you don't have an instance of an Entity you can simply call the static method on the `DB` class like so:

```csharp
var number = await DB.NextSequentialNumberAsync<Person>();
```

#### Generation for any sequence name
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
