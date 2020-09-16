### Embedded Relationships
#### One-to-one:

```csharp
    var author = new Author { Name = "Eckhart Tolle" }
    await author.SaveAsync();
    
    book.Author = author;
    await book.SaveAsync()
```
as mentioned earlier, calling `SaveAsync()` persists `author` to the "Authors" collection in the database. it is also stored in `book.Author` property. so, the `author` entity now lives in two locations (in the collection and also inside the `book` entity) and are linked by the `ID`. if the goal is to embed something as an independant/unlinked document, it is best to use a class that does not inherit from the `Entity` class or simply use the `.ToDocument()` method of an entity as explained earlier.

###### Embed Removal:
to remove the embedded `author`, simply do:
```csharp
	book.Author = null;
	await book.SaveAsync();
```
the original `author` in the `Authors` collection is unaffected.

###### Entity Deletion:
if you call `book.Author.DeleteAsync()`, the author entity is deleted from the `Authors` collection if it was a linked entity.

#### One-to-many:

```csharp
    book.OtherAuthors = new Author[] { author1, author2 };
    await book.SaveAsync();
```
> **Tip:** If you are going to store more than a handful of entities within another entity, it is best to store them by reference as described below.

###### Embed Removal:
```csharp
    book.OtherAuthors = null;
    await book.SaveAsync();
```
the original `author1, author2` entities in the `Authors` collection are unaffected.

###### Entity Deletion:
if you call `book.OtherAuthors.DeleteAllAsync()` the respective `author1, author2` entities are deleted from the `Authors` collection if they were linked entities.



### Referenced Relationships

referenced relationships require a bit of special handling. a **one-to-one** relationship is defined by using the `One<T>` class and **one-to-many** as well as **many-to-many** relationships are defined by using the `Many<T>` class. it is also a good idea to initialize the `Many<T>` properties in the constructor of the parent entity as shown below in order to avoid null-reference exceptions during runtime.
```csharp
    public class Book : Entity
    {
        public One<Author> MainAuthor { get; set; }
        public Many<Author> Authors { get; set; }

        [OwnerSide]
        public Many<Genre> AllGenres { get; set; }

        public Book()
        {
            this.InitOneToMany(() => Authors);
            this.InitManyToMany(() => AllGenres, genre => genre.AllBooks);
        }
    }
```
notice the parameters of the `InitOneToMany` and `InitManyToMany` methods above. the first method only takes one parameter which is just a lambda pointing to the property you want to initialize.

the next method takes 2 parameters. first is the property to initialize. second is the property of the other side of the relationship.

also note that you specify which side of the relationship a property is by using the attributes `[OwnerSide]` or `[InverseSide]` for defining many-to-many relationsips.

#### One-to-one:

call the `ToReference()` method of the entity you want to store as a reference like so:

```csharp
    book.MainAuthor = author.ToReference();
    await book.SaveAsync();
```
alternatively you can use the implicit operator functionality by simply assigning an instance or the string ID like so:
```csharp
  book.MainAuthor = author;
  book.MainAuthor = author.ID;
```

###### Reference Removal:
```csharp
    book.MainAuthor = null;
    await book.SaveAsync();
```
the original `author` in the `Authors` collection is unaffected.

###### Entity Deletion:
If you delete an entity that is referenced as above by calling `author.DeleteAsync()` all references pointing to that entity are automatically deleted. as such, `book.MainAuthor.ToEntityAsync()` will then return `null`. the `.ToEntityAsync()` method is described below.

#### One-to-many & many-to-many:
```charp
    await book.Authors.AddAsync(author); //one-to-many
    await book.AllGenres.AddAsync(genre); //many-to-many
```
there's no need to call `book.SaveAsync()` because references are automatically saved using special join collections. you can read more about them in the [Schema Changes](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/08.-Schema-Changes#reference-collections) section.

###### Reference Removal:
```charp
    await book.Authors.RemoveAsync(author);
    await .AllGenres.RemoveAsync(genre);
```

the original `author` in the `Authors` collection is unaffected. also the `genre` entity in the `Genres` collection is unaffected. only the relationship between entities are deleted.

###### Entity Deletion:
If you delete an entity that is referenced as above by calling `author.DeleteAsync()` all references pointing to that `author` entity are automatically deleted. as such, `book.Authors` will not have `author` as a child. the same applies to `Many-To-Many` relationships. deleting any entity that has references pointing to it from other entities results in those references getting deleted and the relationships being invalidated.

#### ToEntityAsync() shortcut:

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

#### Transaction support
adding and removing related entities require passing in the session when used within a transaction. see [here](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/06.-Transactions#relationship-manipulation) for an example.

### [Next Page >>](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/04.-Queries)