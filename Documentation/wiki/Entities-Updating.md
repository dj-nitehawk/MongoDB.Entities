# Updating without retrieving
you can update a single or batch of entities on the mongodb server by supplying a filter criteria and a subset of properties and the data/ values to be set on them as shown below.
```csharp
await DB.Update<Author>()
        .Match(a => a.Surname == "Stark")
        .Modify(a => a.Name, "Brandon")
        .Modify(a => a.Surname, "The Broken")
        .ExecuteAsync();
```
specify the filter criteria with a lambda expression using the `.Match()` method to indicate which entities/documents you want to target for the update. then use multiples of the `.Modify()` method to specify which properties you want updated with what data. finally call the `.ExecuteAsync()` method to run the update command which will take place remotely on the database server.

if you'd like to update a single entity, simply target it by `.ID` like below:
```csharp
await DB.Update<Author>()
        .MatchID("xxxxxxxxxxx")
        .Modify(a => a.Surname, "The Broken")
        .ExecuteAsync();
```

you can also use filters to match entities. all of the filters in the official driver is available for use as follows.
```csharp
await DB.Update<Author>()
        .Match(f=> f.Eq(a=>a.Surname,"Stark") & f.Gt(a=>a.Age,35))
        .Modify(a => a.Name, "Brandon")
        .ExecuteAsync();
```

also you can use all the _update definition builder_ methods supplied by the mongodb driver like so:
```csharp
await DB.Update<Author>()
        .Match(a => a.ID == "xxxxxxx")
        .Modify(x => x.Inc(a => a.Age, 10))
        .Modify(x => x.Set(a => a.Name, "Brandon"))
        .Modify(x => x.CurrentDate(a => a.ModifiedOn))
        .ExecuteAsync();
```

# Bulk updates
```csharp
var bulkUpdate = DB.Update<Author>();

bulkUpdate.Match(a => a.Age > 25)
          .Modify(a => a.Age, 35)
          .AddToQueue();

bulkUpdate.Match(a => a.Sex == "Male")
          .Modify(a => a.Sex, "Female")
          .AddToQueue();

await bulkUpdate.ExecuteAsync();
```
first get a reference to a `Update<T>` class. then specify matching criteria with `Match()` method and modifications with `Modify()` method just like you would with a regular update. then instead of calling `ExecuteAsync()`, simply call `AddToQueue()` in order to queue it up for batch execution. when you are ready commit the updates, call `ExecuteAsync()` which will issue a single `bulkWrite` command to the database.

# Update and retrieve

in order to update an entity and retrieve the updated enity, use the `.UpdateAndGet<T>()` method on the `DB` class like so:

```csharp
var result = await DB.UpdateAndGet<Book>()
                      .Match(b => b.ID == "xxxxxxxxxxxxx")
                      .Modify(b => b.Title, "updated title")
                      .ExecuteAsync();
```

projection of the returned entity is also possible by using the `.Project()` method before calling `.ExecuteAsync()`. 

# Property preserving updates
if you'd like to skip one or more properties while saving a complete entity, you can do so with the `SavePreservingAsync()` method.
```csharp
await book.SavePreservingAsync(x => new { x.Title, x.Price })
```
this method will build an update command dynamically using reflection and omit the properties you specify. all other properties will be updated in the database with the values from your entity. sometimes, this would be preferable to specifying each and every property with an update command.

> **NOTE:** you should only specify root level properties with the `New` expression. i.e. `x => x.Author.Name` is not valid.

alternatively, you can decorate the properties you want to omit with the `[Preserve]` attribute and simply call `book.SavePreservingAsync()` without supplying an expression. if you specify ommissions using both an expression and attributes, the expression will take precedence and the attributes are ignored.

you can also do the opposite with the use of `[DontPreserve]` attribute. if you decorate properties with `[DontPreserve]`, only the values of those properties are written to the database and all other properties are implicitly ignored when calling `SavePreservingAsync()`. also, the same rule applies that attributes are ignored if you supply a `new` expression to `SavePreservingAsync()`.

> **NOTE:** both `[DontPreserve]` and `[Preserve]` cannot be used together on the same entity type due to the conflicting nature of what they do.

# Aggregation pipeline updates
starting from mongodb sever v4.2, we can refer to existing fields of the documents when updating as described [here](https://docs.mongodb.com/master/reference/command/update/index.html#update-with-aggregation-pipeline).

the following example does 3 things.
- creates a 'FullName' field by concatenating the values from 'FirstName' and 'LastName' fields.
- creates a 'LowerCaseEmail' field by getting the value from 'Email' field and lower-casing it.
- removes the Email field.

```csharp
await DB.Update<Author>()
        .Match(_ => true)
        .WithPipelineStage("{ $set: { FullName: { $concat: ['$FirstName',' ','$LastName'] }}}")
        .WithPipelineStage("{ $set: { LowerCaseEmail: { $toLower: '$Email' } } }")
        .WithPipelineStage("{ $unset: 'Email'}")
        .ExecutePipelineAsync();
```
**note:** pipeline updates and regular updates cannot be used together in one command as it's not supported by the official c# driver.

# Array filter updates
```csharp
await DB.Update<Book>()
        .Match(_ => true)
        .WithArrayFilter("{ 'x.Age' : { $gte : 30 } }")
        .Modify("{ $set : { 'Authors.$[x].Age' : 25 } }")
        .ExecuteAsync();
```
the above update command will set the age of all authors of books where the age is 30 years or older to 25. refer to [this document](https://docs.mongodb.com/manual/reference/operator/update/positional-filtered/) for more info on array filters.