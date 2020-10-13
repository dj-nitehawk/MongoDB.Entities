# Define Entities
This category showcases differnt ways of defining entities and other handy methods.

### Basic entity
```csharp
public class Book : Entity
{
    public string Title { get; set; }
}
```

### Ignore a property when saving
```csharp
public class Book : Entity
{
    public string Title { get; set; }
    [Ignore] public int SomeProperty { get; set; }
}
```

### Customize the collection name for an entity type
```csharp
[Name("Publication")]
public class Book : Entity
{
    ...
}
```

### Automatically set creation date
```csharp
public class Book : Entity, ICreatedOn
{
    public string Title { get; set; }
    public DateTime CreatedOn { get; set; }
}
```

### Automatically set modified date
```csharp
public class Book : Entity, IModifiedOn
{
    public string Title { get; set; }
    public DateTime ModifiedOn { get; set; }
}
```

### Store a properties as ObjectId in the database
```csharp
public class Book : Entity
{
    [ObjectId] public string AuthorID { get; set; }
    [ObjectId] public string[] EditorIDs { get; set; }
}
```

### BYO entities
```csharp
[BsonIgnoreExtraElements]
public class BookEntity : IEntity
{
    [BsonId, ObjectId]
    public string ID { get; set; }
    ...
}
```

### Get the collection for an entity type
```csharp
IMongoCollection<Book> collection = DB.Collection<Book>();
```

### Get the collection name for an entity type
```csharp
string collectionName = DB.CollectionName<Book>();
```

### Entity creation factory
```csharp
Book book = DB.Entity<Book>();
```

### Entity creation factory with ID
```csharp
Book book = DB.Entity<Book>("ID");
```

### Generate a new ObjectId string
```csharp
string objectIdString = DB.NewID()
```