using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;

namespace MongoDB.Entities.Tests
{
    public class MyDB : DBContext
    {
        protected override void OnBeforePersist<T>(IEnumerable<T> entities, UpdateBase<T> update)
        {
            if (typeof(T) == typeof(Flower))//handle specific entity type
            {
                foreach (var flower in entities.As<Flower>())
                {
                    if (flower.ID is null) //handle entity inserts
                    {
                        flower.CreatedDate = DateTime.UtcNow;
                        flower.CreatedBy = "God";
                    }
                    else //handle entity saves
                    {
                        flower.UpdateDate = DateTime.UtcNow;
                        flower.UpdatedBy = "Human";
                    }
                }

                var command = update?.As<Flower>();
                command?.AddModification(f => f.UpdateDate, DateTime.UtcNow);
                command?.AddModification(f => f.UpdatedBy, "Human");
            }
            else
            {

            }
        }
    }
}
