# Multiple database support

Store and retrieve entities in multiple databases on one or more servers. A database instance is identified by its client settings and database name. Different clients can use the same database name; use `DB.Instance(name, clientSettings)` to select the intended instance.

## Tenant per database example

Use the intended `DB` instance for each operation:

```csharp
var db1 = await DB.InitAsync("Customer1");
var db2 = await DB.InitAsync("Customer2");

var auto1 = new Auto { Make = "Toyota", Model = "Corolla", Year = 2020 };
await db1.SaveAsync(auto1);

var auto2 = new Auto { Make = "Honda", Model = "Civic", Year = 2021 };
await db2.SaveAsync(auto2);

var res1 = await db1.Find<Auto>().MatchID(auto1.ID).ExecuteSingleAsync();
var res2 = await db2.Find<Auto>().MatchID(auto2.ID).ExecuteSingleAsync();

// Neither database contains the other tenant's entity.
var missing1 = await db1.Find<Auto>().MatchID(auto2.ID).ExecuteSingleAsync();
var missing2 = await db2.Find<Auto>().MatchID(auto1.ID).ExecuteSingleAsync();
// missing1 and missing2 are null.
```

The example assumes an `Auto : Entity` model with `Make`, `Model`, and `Year` properties.

## Referenced relationships

A `Many<TChild, TParent>` relationship binds to the database supplied when it is initialized. Omitting that argument uses `DB.Default`, even if you later save or query the parent through a different database.

For tenant-specific relationships, bind them explicitly before use. This model deliberately leaves the relationship uninitialized in its parameterless constructor so it does not bind to the default database during deserialization:

```csharp
public class Author : Entity
{
    public string Name { get; set; } = "";
    public Many<Book, Author> Books { get; set; } = null!;
}

public class Book : Entity
{
    public string Title { get; set; } = "";
}
```

```csharp
var tenantDb = DB.Instance("Customer2");
var author = new Author { Name = "Tenant author" };
var book = new Book { Title = "Tenant book" };

author.InitOneToMany(() => author.Books, tenantDb);
await tenantDb.SaveAsync(author);
await tenantDb.SaveAsync(book);
await author.Books.AddAsync(book);

// Rebind after loading a new parent instance, before accessing its relationship.
var loadedAuthor = await tenantDb.Find<Author>().OneAsync(author.ID);
if (loadedAuthor is not null)
{
    loadedAuthor.InitOneToMany(() => loadedAuthor.Books, tenantDb);
    var books = await loadedAuthor.Books.ChildrenQueryable().ToListAsync();
}
```

Import `MongoDB.Driver.Linq` for the async LINQ extension above. For many-to-many relationships, pass the same tenant database to `InitManyToMany` on both sides. For `One<T>` references, pass the intended database to `ToEntityAsync(db)`.

## Limitations

- Parents, children, and join collections of a `Many<TChild, TParent>` relationship must be in the same database.
- Relationship joins and lookups do not span databases.
- Avoid changing `DB.Default` to route concurrent tenant operations. Use explicit database instances and relationship bindings.
