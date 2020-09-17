# Define entities

add the import statement shown below and create your entities by inheriting the `Entity` base class.

```csharp
using MongoDB.Entities;

public class Book : Entity
{
    public string Title { get; set; }
}
```

# Ignore properties

if there are some properties on entities you don't want persisted to mongodb, simply use the `IgnoreAttribute` 
```csharp
public class Book : Entity
{
    [Ignore]
    public string SomeProperty { get; set; }
}
```

# Customize collection names
by default, mongodb collections will use the names of the entity classes. you can customize the collection names by decorating your entities with the `NameAttribute` as follows:
```csharp
[Name("Writer")]
public class Author : Entity
{
    ...
}
```

# Optional auto-managed properties
there are 2 optional interfaces `ICreatedOn` & `IModifiedOn` that you can add to entity class definitions like so:
```csharp
public class Book : Entity, ICreatedOn, IModifiedOn
{
    public string Title { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}
```
if your entity classes implements these interfaces, the library will automatically set the appropriate values so you can use them for sorting operations and other queries.

# The IEntity interface

if for whatever reason, you're unable to inherit the `Entity` base class, you can simply implement the `IEntity` interface to make your classes compatible with the library like so:
```csharp
[BsonIgnoreExtraElements]
public class Book : IEntity
{
    [BsonId, ObjectId]
    public string ID { get; set; }
    ...
}
```