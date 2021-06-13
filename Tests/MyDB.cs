using MongoDB.Entities.Tests.Models;
using System;

namespace MongoDB.Entities.Tests
{
    public class MyDB : DBContext
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
}
