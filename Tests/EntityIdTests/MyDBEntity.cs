using System;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

public class MyDBTemplatesEntity : DBContext
{
    public MyDBTemplatesEntity(bool prepend) : base(modifiedBy: new())
    {
        SetGlobalFilter(typeof(AuthorEntity), "{ Age: {$eq: 111 } }", prepend);
    }
}

public class MyDBEntity : DBContext
{
    public MyDBEntity(bool prepend = false) : base(modifiedBy: new())
    {
        SetGlobalFilter<AuthorEntity>(a => a.Age == 111, prepend);
    }

    protected override Action<T> OnBeforeSave<T>()
    {
        Action<FlowerEntity> action = f =>
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
        Action<UpdateBase<FlowerEntity>> action = update =>
        {
            update.AddModification(f => f.UpdatedBy, "Human");
            update.AddModification(f => f.UpdateDate, DateTime.UtcNow);
        };

        return (action as Action<UpdateBase<T>>)!;
    }
}

public class MyDBFlower : DBContext
{
    public MyDBFlower(bool prepend)
    {
        SetGlobalFilterForInterface<ISoftDeleted>("{IsDeleted:false}", prepend);
    }
}

public class MyBaseEntityDB : DBContext
{
    public MyBaseEntityDB()
    {
        SetGlobalFilterForBaseClass<BaseEntity>(be => be.CreatedBy == "xyz");
    }
}
