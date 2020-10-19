using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Tests.Models
{
    public class Customer : IEntity
    {
        [BsonId]
        public string ID { get; set; }

        public void SetNewID()
        {
            ID = $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";
        }
    }
}
