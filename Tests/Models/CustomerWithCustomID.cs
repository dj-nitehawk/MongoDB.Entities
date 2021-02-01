using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Tests.Models
{
    public class CustomerWithCustomID : IEntity
    {
        [BsonId]
        public string Id { get; set; }

        public string GenerateNewId()
            => $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";
    }
}
