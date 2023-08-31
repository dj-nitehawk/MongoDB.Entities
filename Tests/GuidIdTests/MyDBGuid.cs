using MongoDB.Entities.Tests.Models;
using System;

namespace MongoDB.Entities.Tests;

public class MyDBTemplatesGuid : DBContext
{
    public MyDBTemplatesGuid(bool prepend) : base(modifiedBy: new Entities.ModifiedBy())
    {
        SetGlobalFilter(typeof(AuthorUuid), "{ Age: {$eq: 111 } }", prepend);
    }
}

public class MyDBGuid : DBContext
{
    public MyDBGuid(bool prepend = false) : base(modifiedBy: new Entities.ModifiedBy())
    {
        SetGlobalFilter<AuthorUuid>(a => a.Age == 111, prepend);
    }

    protected override Action<T> OnBeforeSave<T>()
    {
        Action<FlowerUuid> action = f =>
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
        Action<UpdateBase<FlowerUuid>> action = update =>
        {
            update.AddModification(f => f.UpdatedBy, "Human");
            update.AddModification(f => f.UpdateDate, DateTime.UtcNow);
        };

        return (action as Action<UpdateBase<T>>)!;
    }
}

