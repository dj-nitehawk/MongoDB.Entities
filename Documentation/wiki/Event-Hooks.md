# Custom event hooks

There are two hooks you can tap into: `OnBeforeSave` and `OnBeforeUpdate`. Use them to modify the incoming operation just before it executes. These hooks can also replace the built-in audit fields for finer-grained control. Override both methods to ensure inserts and updates are consistently handled.

Say for example, you have a `Flower` entity like the following, and you want to automatically set the creator/date when new flowers are being persisted and also modify the updater/date when existing entities get updated.

```csharp
public class Flower : Entity
{
    public string Name { get; set; }

    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public string UpdatedBy { get; set; }
    public DateTime UpdateDate { get; set; }
}
```

To be able to tap in to the hooks, create a derived `DB` class and override the two methods as follows:

```csharp
public class MyDatabase() : DB(Default) //example of using the default database
{
    protected override Action<T>? OnBeforeSave<T>()
    {
        return ModifyFlowerEntities as Action<T>;

        void ModifyFlowerEntities(Flower f)
        {
            if (string.IsNullOrEmpty(f.ID))
            {
                f.CreatedBy = "God";
                f.CreatedDate = DateTime.MinValue;
            }
            else
            {
                f.UpdatedBy = "Human";
                f.UpdateDate = DateTime.UtcNow;
            }
        }
    }

    protected override Action<UpdateBase<T>>? OnBeforeUpdate<T>()
    {
        return ModifyFlowerEntities as Action<UpdateBase<T>>;

        void ModifyFlowerEntities(UpdateBase<Flower> update)
        {
            update.AddModification(f => f.UpdatedBy, "Human");
            update.AddModification(f => f.UpdateDate, DateTime.UtcNow);
        }
    }
}
```

After that, simply create new instances of `MyDatabase` when you need the above functionality and perform operations as usual:

```csharp
var db = new MyDatabase();

await db.SaveAsync(new Flower() { Name = "Red Rose" });

await db.Update<Flower>()
        .Match(f => f.Name == "Red Rose")
        .Modify(f => f.Name, "White Rose")
        .ExecuteAsync();
```

## Handling multiple entity types

It's possible to handle more than one type of entity inside the hooks like below:

```csharp
protected override Action<T>? OnBeforeSave<T>()
{
    return typeof(T) switch
    {
        Type t when t == typeof(Book) => BookModifier as Action<T>,
        Type t when t == typeof(Flower) => FlowerModifier as Action<T>,
        _ => null
    };

    void BookModifier(Book b)
    {
        b.SavedBy = "Author";
        b.SavedOn = DateTime.UtcNow;
    }

    void FlowerModifier(Flower f)
    {
        f.SavedBy = "Human";
        f.SavedOn = DateTime.MinValue;
    }
}
```