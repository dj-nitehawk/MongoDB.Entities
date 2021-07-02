# Referenced Relationships

referenced relationships require a bit of special handling. a **one-to-one** relationship is defined using the `One<T>` class and **one-to-many** as well as **many-to-many** relationships are defined using the `Many<T>` class and you have to initialize the `Many<T>` child properties in the constructor of the parent entity as shown below.
```csharp
public class Book : Entity
{
    public One<Author> MainAuthor { get; set; }
    
    public Many<Author> CoAuthors { get; set; }
    
    [OwnerSide] 
    public Many<Genre> Genres { get; set; }

    public Book()
    {
        this.InitOneToMany(() => CoAuthors);
        this.InitManyToMany(() => Genres, genre => genre.Books);
    }
}

public class Genre : Entity
{
    [InverseSide] 
    public Many<Book> Books { get; set; }

    public Genre()
    {
        this.InitManyToMany(() => Books, book => book.Genres);
    }
}
```
notice the parameters of the `InitOneToMany` and `InitManyToMany` methods above. the first method only takes one parameter which is just a lambda pointing to the property you want to initialize.

the next method takes 2 parameters. first is the property to initialize. second is the property of the other side of the relationship.

also note that you specify which side of the relationship a property is using the attributes `[OwnerSide]` or `[InverseSide]` for defining many-to-many relationsips.

## One-to-one

a reference can be assigned in any of the following three ways:

```csharp
book.MainAuthor = author.ToReference(); //call ToReference on a child
book.MainAuthor = author;               //assign a child instance
book.MainAuthor = "AuthorID";           //assign just the ID value of a child

await book.SaveAsync();                 //call save on parent to store
```

### Reference removal
```csharp
book.MainAuthor = null;
await book.SaveAsync();
```
the original `author` in the `Authors` collection is unaffected.

### Entity deletion

if you delete an entity that is referenced as above, all references pointing to that entity would then be invalid. as such, `book.MainAuthor.ToEntityAsync()` will then return `null`. the `.ToEntityAsync()` method is described below.

for example:

```
book A has 1:1 relationship with author A
book B has 1:1 relationship with author A
book C has 1:1 relationship with author A

now, if you delete author A, the results would be the following:

await bookA.MainAuthor.ToEntityAsync() //returns null
await bookB.MainAuthor.ToEntityAsync() //returns null
await bookC.MainAuthor.ToEntityAsync() //returns null
```

## One-to-many & many-to-many
```csharp
await book.Authors.AddAsync(author); //one-to-many
await book.Genres.AddAsync(genre); //many-to-many
```
there's no need to call `book.SaveAsync()` again because references are automatically saved using special join collections. you can read more about them in the [Schema Changes](Schema-Changes.md#reference-collections) section.

however, do note that both the parent entity (book) and child (author/genre) being added has to have been previously saved so that they have their `ID` values populated. otherwise, you'd get an exception instructing you to save them both before calling `AddAsync()`.

alternatively when you don't have access to the parent entity and you only have the parent `ID` value, you can use the following to access the relationship:

```csharp
await DB.Entity<Book>("BookID").Authors.AddAsync(author);
```

there are other *[overloads](xref:MongoDB.Entities.Many`1#methods)* for adding relationships with multiple entities or just the string IDs.

[click here](https://gist.github.com/dj-nitehawk/9971a57062f32fac8e7597a889d47714) to see a full example of a referenced one-to-many relationship.

### Reference removal
```csharp
await book.Authors.RemoveAsync(author);
await book.Genres.RemoveAsync(genre);
```

the original `author` in the `Authors` collection is unaffected. also the `genre` entity in the `Genres` collection is unaffected. only the relationship between entities are deleted.

there are other *[overloads](xref:MongoDB.Entities.Many`1.RemoveAsync(`0,IClientSessionHandle,CancellationToken))* for removing relationships with multiple entities or just the string IDs.

### Entity deletion
when you delete an entity that's in a `one-to-many` or `many-to-many` relationship, all the references (join records) for the relationship in concern are automatically deleted from the join collections.

for example:

```
| author A has 3 referenced books:
|-- book A
|-- book B
|-- book C

| author B has 3 referenced book:
|-- book A
|-- book B
|-- book C

now, if you delete book B, the children of authors A and B would look like this:

| author A:
|-- book A
|-- book C

| author B:
|-- book A
|-- book C
```

# ToEntityAsync() shortcut

a reference can be turned back in to an entity with the `ToEntityAsync()` method.

```csharp
var author = await book.MainAuthor.ToEntityAsync();
```
you can also project the properties you need instead of getting back the complete entity like so:
```csharp
var author = await book.MainAuthor
                       .ToEntityAsync(a => new Author
                        {
                          Name = a.Name,
                          Age = a.Age
                        });
```

# Transaction support
adding and removing related entities require passing in the session when used within a transaction. see [here](Transactions.md#relationship-manipulation) for an example.
