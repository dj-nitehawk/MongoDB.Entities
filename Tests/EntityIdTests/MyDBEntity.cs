using System;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

public class MyDBTemplatesEntity : DB
{
    public MyDBTemplatesEntity(bool prepend) : base(Default)
    {
        ModifiedBy = new();
        SetGlobalFilter(typeof(AuthorEntity), "{ Age: {$eq: 111 } }", prepend);
    }
}

public class MyDBEntity : DB
{
    public MyDBEntity(bool prepend = false) : base(Default)
    {
        ModifiedBy = new();
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

public class MyDBFlower : DB
{
    public MyDBFlower(bool prepend) : base(Default)
    {
        SetGlobalFilterForInterface<ISoftDeleted>("{IsDeleted:false}", prepend);
    }
}

public class MyBaseEntityDB : DB
{
    public MyBaseEntityDB() : base(Default)
    {
        SetGlobalFilterForBaseClass<BaseEntity>(be => be.CreatedBy == "xyz");
    }
}