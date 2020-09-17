# Update without retrieving
you can update a single or batch of entities on the mongodb server by supplying a filter criteria and a subset of properties and the data/ values to be set on them as shown below.
```csharp
await DB.Update<Author>()
        .Match(a => a.Surname == "Stark")
        .Modify(a => a.Name, "Brandon")
        .Modify(a => a.Surname, "The Broken")
        .ExecuteAsync();
```
specify the filter criteria with a lambda expression using the `.Match()` method to indicate which entities/documents you want to target for the update. then use multiples of the `.Modify()` method to specify which properties you want updated with what data. finally call the `.ExecuteAsync()` method to run the update command which will take place remotely on the database server.

## Update by ID
if you'd like to update a single entity, simply target it by `ID` like below:
```csharp
await DB.Update<Author>()
        .MatchID("xxxxxxxxxxx")
        .Modify(a => a.Surname, "The Broken")
        .ExecuteAsync();
```

## Update by matching with filters
you can use [_filter definition builder_](https://mongodb.github.io/mongo-csharp-driver/2.11/apidocs/html/Methods_T_MongoDB_Driver_FilterDefinitionBuilder_1.htm) methods to match entities. all of the filters of the official driver are available for use as follows.
```csharp
await DB.Update<Author>()
        .Match(f=> f.Eq(a=>a.Surname,"Stark") & f.Gt(a=>a.Age,35))
        .Modify(a => a.Name, "Brandon")
        .ExecuteAsync();
```

## Update with builder methods
also you can use all the [_update definition builder_](https://mongodb.github.io/mongo-csharp-driver/2.11/apidocs/html/Methods_T_MongoDB_Driver_UpdateDefinitionBuilder_1.htm) methods supplied by the mongodb driver like so:
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
## Update and retrieve with projection
projection of the returned entity is also possible by using the `.Project()` method before calling `.ExecuteAsync()`. 
```csharp
var result = await DB.UpdateAndGet<Book>()
                     .Match(b => b.ID == "xxxxxxxxxxxxx")
                     .Modify(b => b.Title, "updated title")
                     .Project(b => new Book { Title = b.Title })
                     .ExecuteAsync();
```

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