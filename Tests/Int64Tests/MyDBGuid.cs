using MongoDB.Entities.Tests.Models;
using System;

namespace MongoDB.Entities.Tests;

public class MyDBTemplatesInt64 : DBContext
{
    public MyDBTemplatesInt64(bool prepend) : base(modifiedBy: new Entities.ModifiedBy())
    {
        SetGlobalFilter(typeof(AuthorInt64), "{ Age: {$eq: 111 } }", prepend);
    }
}

public class MyDBInt64 : DBContext
{
    public MyDBInt64(bool prepend = false) : base(modifiedBy: new Entities.ModifiedBy())
    {
        SetGlobalFilter<AuthorInt64>(a => a.Age == 111, prepend);
    }

    protected override Action<T> OnBeforeSave<T>()
    {
        Action<FlowerInt64> action = f =>
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
        Action<UpdateBase<FlowerInt64>> action = update =>
        {
            update.AddModification(f => f.UpdatedBy, "Human");
            update.AddModification(f => f.UpdateDate, DateTime.UtcNow);
        };

        return (action as Action<UpdateBase<T>>)!;
    }
}
