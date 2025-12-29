# Define entities

add the import statement and create your entities by inheriting the `Entity` base class.

```csharp
using MongoDB.Entities;

public class Book : Entity
{
    public string Title { get; set; }
}
```

# Ignore properties

if there are some properties on entities you don't want persisted to mongodb, simply use the `IgnoreAttribute`.
you can prevent null/default values from being stored with the use of `IgnoreDefaultAttribute`.

```csharp
public class Book : Entity
{
    [Ignore] 
    public string DontSaveMe { get; set; }

    [IgnoreDefault] 
    public int SomeNumber { get; set; }
}
```

# Customize field names

you can set the field names of the documents stored in mongodb using the `FieldAttribute` like so:

```csharp
public class Book
{
    [Field("book_name")]
    public string Title { get; set; }
}
```

# Customize collection names

by default, mongodb collections will use the names of the entity classes. you can customize the collection names by decorating your entities with the `CollectionAttribute` as follows:

```csharp
[Collection("Writer")]
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
public class Book : IEntity
{
    public string ID { get; set; } = null!;

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();
}
```

# Customizing the ID format

the default format of the IDs automatically generated for new entities is `ObjectId`. if you'd like to change the type/format of the ID, simply override the `GenerateNewID` method of the `Entity` base class or implement the `IEntity` interface. if implementing `IEntity`, don't forget to decorate the ID property with the `[BsonId]` attribute to indicate that it's the primary key.

```csharp
public class Book : IEntity
{
    [BsonId]
    public Guid Id { get; set; } = Guid.Empty;

    public object GenerateNewID()
        => Guid.NewGuid();
}
```

> [!note]
> the type of the ID property can be whatever type you like (given that it can be serialized by the mongo driver). however, due to a technical constraint, only the following types are supported with the [referenced relationship](Relationships-Referenced.md) functionality:
>
> - string
> - long
> - ObjectId

<!-- <h2 style="color:#cb0000">A word of warning about custom IDs</h2> -->
> [!warning]
> it is highly recommended that you stick with `ObjectId` as it's highly unlikely it would generate duplicate IDs due to [the way it works](https://www.mongodb.com/blog/post/generating-globally-unique-identifiers-for-use-with-mongodb).
>
>
>if you choose something like `Guid`, there's a possibility for duplicates to be generated and data loss could occur when using the [partial entity saving](Entities-Save.md#save-entities-partially) operations. reason being, those operations use upserts under the hood and if a new entity is assigned the same ID as one that already exists in the database, the existing entity will get replaced by the new entity.
>
>
>the normal save operations do not have this issue because they use inserts under the hood and if you try to insert a new entity with a duplicate ID, a duplicate key exception would be thrown due to the unique index on the ID property.
>
>
>so you're better off sticking with `ObjectId` because the only way it could ever generate a duplicate ID is if more than 16 million entities are created at the exact moment on the exact computer with the exact same process.

# Create a collection explicitly

```csharp
await db.CreateCollectionAsync<Book>(o =>
{
    o.Collation = new Collation("es");
    o.Capped = true;
    o.MaxDocuments = 10000;
});
```

typically you don't need to create collections manually as they will be created automatically the first time you save an entity.
however, you'd have to create the collection like above if you need to use a custom *[COLLATION](https://docs.mongodb.com/manual/reference/collation/)*, create a *[CAPPED](https://docs.mongodb.com/manual/core/capped-collections/)*, or *[TIME SERIES](https://docs.mongodb.com/manual/core/timeseries-collections/)* collection before you can save any entities.

> [!note]
> if a collection already exists for the specified entity type, an exception will be thrown.

# Drop a collection

```csharp
await db.DropCollectionAsync<Book>();
```