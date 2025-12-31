# Migration system

There's a simple data migration system similar to that of EntityFramework where you can write migration classes with logic for transforming the database and content in order to bring it up-to-date with the current shape of your c# entity schema.

### Migration classes

Create migration classes that has names starting with `_number_` followed by anything you'd like and implement the interface `IMigration`.

Here are a couple of valid migration class definitions:

```csharp
public class _001_i_will_be_run_first : IMigration { }
public class _002_i_will_be_run_second : IMigration { }
public class _003_i_will_be_run_third : IMigration { }
```

Next implement the `UpgradeAsync()` method of IMigration and place your migration logic there.

### Run Migrations

In order to execute the migrations, simply call `db.MigrateAsync()` whenever you need the database brought up to date. The library keeps track of the last migration run and will execute all newer migrations in the order of their number. In most cases, you'd place the following line of code in the startup of your app right after initializing the database.

```csharp
await db.MigrateAsync();
```

The above will try to discover all migrations from all assemblies of the application if it's a multi-project solution. You can speed things up a bit by specifying a type so that migrations will only be discovered from the same assembly/project as the specified type, like so:

```csharp
await db.MigrateAsync<SomeType>();
```

It's also possible to have more control by supplying a collection of migration class instances, which comes in handy if your migrations require other dependencies.

```csharp
await db.MigrationsAsync(new IMigration[]
{
    new _001_seed_data(someDependency),
    new _002_transform_data(someDependency)
});
```

### Examples

#### Merge two properties

Let's take the scenario of having the first and last names of an Author entity stored in two separate properties and later on deciding to merge them into a single property called "FullName".

```csharp
public class _001_merge_first_and_last_name_to_fullname_field : IMigration
{
    private class Author : Entity
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string FullName { get; set; }
    }

    public async Task UpgradeAsync()
    {
        await db.Fluent<Author>()
                .Project(a => new { id = a.ID, fullname = a.Name + " " + a.Surname })
                .ForEachAsync(async a =>
                {
                    await db.Update<Author>()
                            .Match(_ => true)
                            .Modify(x => x.FullName, a.fullname)
                            .ExecuteAsync();
                });
    }
}
```

If your collection has many thousands of documents, the above code will be less efficient. Below is another more efficient way to achieve the same result using a single mongodb command if your server version is v4.2 or newer.

```csharp
public class _001_merge_first_and_last_name_to_fullname_field : IMigration
{
    public Task UpgradeAsync()
    {
      return db.Update<Author>()
               .Match(_ => true)
               .WithPipelineStage("{$set:{FullName:{$concat:['$Name',' ','$Surname']}}}")
               .ExecutePipelineAsync();
    }
}
```

#### Rename a property

```csharp
public class _002_rename_fullname_to_authorname : IMigration
{
    public Task UpgradeAsync()
    {
      return db.Update<Author>()
                .Match(_ => true)
                .Modify(b => b.Rename("FullName", "AuthorName"))
                .ExecuteAsync();
    }
}
```

#### Rename a collection

```csharp
public class _003_rename_author_collection_to_writer : IMigration
{
    public Task UpgradeAsync()
    {
      return db.Database()
               .RenameCollectionAsync("Author", "Writer");
    }
}
```