# Schema changes

Be mindful when changing the schema of your entities. The documents/entities stored in mongodb are overwritten with the current schema/ shape of your entities when you call [SaveAsync](Entities-Save.md). For example:

###### Old schema

```csharp
public class Book : Entity
{
    public int Price { get; set; }
}
```

###### New schema

```csharp
public class Book : Entity
{
    public int SellingPrice { get; set; }
}
```

The data stored in mongodb under `Price` will be lost upon saving if you do not manually handle the transfer of data from the old property to the new property.

### Renaming entities

If you for example rename the `Book` entity to `Test` when you run you app, a new collection called "Test" will be created and the old collection called "Book" will be orphaned. Any new entities you save will be added to the "Test" collection. To avoid that, you can simply rename the collection called "Book" to "Test" before running your app. Or you can tie down the name of the collection using the [\[Name\]](Entities.md#customize-collection-names) attribute

### Reference collections

Reference(Join) collections use the naming format `[Parent~Child(PropertyName)]` for **One-To-Many** and `[(PropertyName)Parent~Child(PropertyName)]` for **Many-To-Many**. You don't have to pay any attention to these special collections unless you rename your entities or properties.

For ex: if you rename the `Book` entity to `AwesomeBook` and property holding it to `GoodAuthors` just rename the corresponding join collection from `[Book~Author(Authors)]` to `[AwesomeBook~Author(GoodAuthors)]` in order to get the references working again.

If you need to drop a join collection that is no longer needed, you can delete them like so:

```csharp
await DB.Entity<Author>().Books.JoinCollection.DropAsync();
```

### Indexes

Some care is needed to make sure there won't be any orphaned/ redundant indexes in mongodb after changing your schema.

**Renaming entities**

If you rename an entity, simply rename the corresponding collection in mongodb before running your app as mentioned in the previous section and all indexes will continue to work because indexes are tied to the collections they're in. Or simply tie down the collection name with the [\[Collection\]](Entities.md#customize-collection-names) attribute.

**Changing entity properties or index definitions**

After running the app with changed property names or modified index definitions, new indexes will be automatically created to match the current shape of index definitions in your code. You should manually drop indexes that have old schema in order to get rid of redundant/ orphaned indexes.

> [!note]
> The only exception to the above is text indexes. Text indexes don't require any manual handling. Since there can only be one text index per collection, the library automatically drops and re-creates text indexes when a schema change is detected.

### Migration system

Now that you understand how schema changes affect the database, you can automate the needed changes using the newly introduced migration system as explained in the [Data Migrations](Data-Migrations.md) section.