using MongoDB.Entities.Tests.Models;
using System;

namespace MongoDB.Entities.Tests
{
    public class MyDBTemplates : DBContext
    {
        public MyDBTemplates(bool prepend) : base(modifiedBy: new Entities.ModifiedBy())
        {
            SetGlobalFilter(typeof(Author), "{ Age: {$eq: 111 } }", prepend);
        }
    }

    public class MyDB : DBContext
    {
        public MyDB(bool prepend = false) : base(modifiedBy: new Entities.ModifiedBy())
        {
            SetGlobalFilter<Author>(a => a.Age == 111, prepend);
        }

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
}
