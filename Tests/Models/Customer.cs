using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Tests.Models
{
    public class Customer : IEntity
    {
        [BsonId]
        public string ID { get; set; }

        static Customer()
        {
            DB.IDGenerationLogicFor<Customer>(
                () => $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}");
        }
    }
}
