# Custom event hooks

Override `OnBeforeSave` and `OnBeforeUpdate` on a derived `DB` to modify entities and update definitions before writes. For example, record the last editor and save time:

```csharp
public class Flower : Entity
{
    public string Name { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public DateTime UpdateDate { get; set; }
}
```

```csharp
public class MyDatabase() : DB(Default)
{
    protected override Action<T>? OnBeforeSave<T>()
    {
        return entity =>
        {
            if (entity is Flower flower)
            {
                flower.UpdatedBy = "Human";
                flower.UpdateDate = DateTime.UtcNow;
            }
        };
    }

    protected override Action<UpdateBase<T>>? OnBeforeUpdate<T>()
    {
        return update =>
        {
            if (update is UpdateBase<Flower> flowers)
            {
                flowers.AddModification(f => f.UpdatedBy, "Human");
                flowers.AddModification(f => f.UpdateDate, DateTime.UtcNow);
            }
        };
    }
}
```

Use the derived instance to perform the operations:

```csharp
var db = new MyDatabase();

await db.SaveAsync(new Flower { Name = "Red Rose" });

await db.Update<Flower>()
        .Match(f => f.Name == "Red Rose")
        .Modify(f => f.Name, "White Rose")
        .ExecuteAsync();
```

## Pipeline updates

`ExecutePipelineAsync()` on both `Update` and `UpdateAndGet` invokes the action returned by `OnBeforeUpdate` once before writing, including when using `WithModifiedBy()` views. A hook exception prevents the write.

If your hook calls `AddModification`, pipeline execution throws `ArgumentException` before writing, even when the hook modifies a different field from the pipeline. Hook modifications are never silently discarded or translated into pipeline stages. Use regular updates with modifying hooks. Validation-only hooks that do not add modifications can be used with pipeline updates. This restriction applies to hook-added definitions; existing pipeline handling of library-supplied audit definitions is unchanged.

## Detecting new entities

Do not use an empty ID inside `OnBeforeSave` as a general insert detector. Single-entity `SaveAsync` invokes the hook before preparing the ID, while batch saves, partial saves, and `InsertAsync` prepare IDs first. A new entity can therefore already have its ID when the hook runs.

For creation timestamps, use [`ICreatedOn`](Entities.md#optional-auto-managed-properties). For creation-only business logic, track creation explicitly in your application and set the values before saving, rather than inferring it from hook timing.

## Handling multiple entity types

Use type checks inside the returned action. For example, given this additional entity:

```csharp
public class Book : Entity
{
    public string SavedBy { get; set; } = "";
    public DateTime SavedOn { get; set; }
}
```

Replace the save hook with:

```csharp
protected override Action<T>? OnBeforeSave<T>()
{
    return entity =>
    {
        switch (entity)
        {
            case Book book:
                book.SavedBy = "Author";
                book.SavedOn = DateTime.UtcNow;
                break;
            case Flower flower:
                flower.UpdatedBy = "Human";
                flower.UpdateDate = DateTime.UtcNow;
                break;
        }
    };
}
```
