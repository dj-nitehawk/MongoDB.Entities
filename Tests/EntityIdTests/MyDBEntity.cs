using System;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

public class MyDbTemplatesEntity : DB
{
    public MyDbTemplatesEntity(bool prepend) : base(Default)
    {
        ModifiedBy = new();
        SetGlobalFilter(typeof(AuthorEntity), "{ Age: {$eq: 111 } }", prepend);
    }
}

public class MyDbEntity : DB
{
    public MyDbEntity(bool prepend = false) : base(Default)
    {
        ModifiedBy = new();
        SetGlobalFilter<AuthorEntity>(a => a.Age == 111, prepend);
    }

    protected override Action<T> OnBeforeSave<T>()
    {
        Action<FlowerEntity> action = f =>
                                      {
                                          if (string.IsNullOrEmpty(f.Id))
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

public class MyDbFlower : DB
{
    public MyDbFlower(bool prepend) : base(Default)
    {
        SetGlobalFilterForInterface<ISoftDeleted>("{IsDeleted:false}", prepend);
    }
}

public class MyBaseEntityDb : DB
{
    public MyBaseEntityDb() : base(Default)
    {
        SetGlobalFilterForBaseClass<BaseEntity>(be => be.CreatedBy == "xyz");
    }
}