# Custom event hooks

there are currently two hooks for tapping into. `OnBeforeSave` and `OnBeforeUpdate` so that you can perform modifications to the operation that's about to happen. 
it can also be used as an alternative to the pre-baked audit fields functionality for more fine-grain control. make sure to override both methods in order to cover all the bases.

say for example, you have a `Flower` entity like the following and you want to automatically set the creator/date when new flowers are being persisted and also modify the updater/date when existing entities get updated.
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
to be able to tap in to the hooks, create a derived `DBContext` class and override the two methods as follows:
```csharp
public class MyDBContext : DBContext
{
    protected override Action<T> OnBeforeSave<T>()
    {
        Action<Flower> action = f =>
        {
            if (f.ID == null)
            {
                f.CreatedBy = "God";
                f.CreatedDate = DateTime.MinValue;
            }
            else
            {
                f.UpdatedBy = "Human";
                f.UpdateDate = DateTime.UtcNow;
            }
        };
        return action as Action<T>;
    }

    protected override Action<UpdateBase<T>> OnBeforeUpdate<T>()
    {
        Action<UpdateBase<Flower>> action = update =>
        {
            update.AddModification(f => f.UpdatedBy, "Human");
            update.AddModification(f => f.UpdateDate, DateTime.UtcNow);
        };
        return action as Action<UpdateBase<T>>;
    }
}
```
after that, simply create new instances of `MyDBContext` when you need the above functionality and perform operations as usual like so:
```csharp
var db = new MyDBContext();

await db.SaveAsync(new Flower() { Name = "Red Rose" });

await db.Update<Flower>()
        .Match(f => f.Name == "Red Rose")
        .Modify(f => f.Name, "White Rose")
        .ExecuteAsync();
```

## Handling multiple entity types

it's possible to handle more than one type of entity inside the hooks like below:
```csharp
protected override Action<T> OnBeforeSave<T>()
{
    var type = typeof(T);

    if (type == typeof(Book))
    {
        Action<Book> action = b =>
        {
            b.SavedBy = "Author";
            b.SavedOn = DateTime.UtcNow;
        };
        return action as Action<T>;
    }

    if (type == typeof(Flower))
    {
        Action<Flower> action = f =>
        {
            f.SavedBy = "Human";
            f.SavedOn = DateTime.MinValue;
        };
        return action as Action<T>;
    }

    return null;
}
```