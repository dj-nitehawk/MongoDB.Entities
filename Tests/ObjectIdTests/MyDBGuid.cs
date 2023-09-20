using System;

namespace MongoDB.Entities.Tests;

public class MyDBTemplatesObjectId : DBContext
{
    public MyDBTemplatesObjectId(bool prepend) : base(modifiedBy: new ModifiedBy())
    {
        SetGlobalFilter(typeof(AuthorObjectId), "{ Age: {$eq: 111 } }", prepend);
    }
}

public class MyDBObjectId : DBContext
{
    public MyDBObjectId(bool prepend = false) : base(modifiedBy: new ModifiedBy())
    {
        SetGlobalFilter<AuthorObjectId>(a => a.Age == 111, prepend);
    }

    protected override Action<T> OnBeforeSave<T>()
    {
        Action<FlowerObjectId> action = f =>
        {
            if (f.Id == null)
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

        return (action as Action<T>)!;
    }

    protected override Action<UpdateBase<T>> OnBeforeUpdate<T>()
    {
        Action<UpdateBase<FlowerObjectId>> action = update =>
        {
            update.AddModification(f => f.UpdatedBy, "Human");
            update.AddModification(f => f.UpdateDate, DateTime.UtcNow);
        };

        return (action as Action<UpdateBase<T>>)!;
    }
}
