# Schema changes

be mindful when changing the schema of your entities. the documents/entities stored in mongodb are overwritten with the current schema/ shape of your entities when you call [SaveAsync](Entities-Save.md). for example:

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

the data stored in mongodb under `Price` will be lost upon saving if you do not manually handle the transfer of data from the old property to the new property.

### Renaming entities

if you for example rename the `Book` entity to `Test` when you run you app, a new collection called "Test" will be created and the old collection called "Book" will be orphaned. Any new entities you save will be added to the "Test" collection. To avoid that, you can simply rename the collection called "Book" to "Test" before running your app. or you can tie down the name of the collection using the [\[Name\]](Entities.html#customize-collection-names) attribute

### Reference collections
Reference(Join) collections use the naming format `[Parent~Child(PropertyName)]` for **One-To-Many** and `[(PropertyName)Parent~Child(PropertyName)]` for **Many-To-Many**. you don't have to pay any attention to these special collections unless you rename your entities or properties. 

for ex: if you rename the `Book` entity to `AwesomeBook` and property holding it to `GoodAuthors` just rename the corresponding join collection from `[Book~Author(Authors)]` to `[AwesomeBook~Author(GoodAuthors)]` in order to get the references working again. 

### Indexes
some care is needed to make sure there won't be any orphaned/ redundant indexes in mongodb after changing your schema.

#### Renaming entities
if you rename an entity, simply rename the corresponding collection in mongodb before running your app as mentioned in the previous section and all indexes will continue to work because indexes are tied to the collections they're in. or simply tie down the collection name with the [\[Name\]](Entities.html#customize-collection-names) attribute.

#### Changing entity properties or index definitions
after running the app with changed property names or modified index definitions, new indexes will be automatically created to match the current shape of index definitions in your code. you should manually drop indexes that have old schema in order to get rid of redundant/ orphaned indexes.

>[!note]
> the only exception to the above is text indexes. text indexes don't require any manual handling. since there can only be one text index per collection, the library automatically drops and re-creates text indexes when a schema change is detected.

### Migration system
now that you understand how schema changes affect the database, you can automate the needed changes using the newly introduced migration system as explained in the [Data Migrations](Data-Migrations.md) section.
