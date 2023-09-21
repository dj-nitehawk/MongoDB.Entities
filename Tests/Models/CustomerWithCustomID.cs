using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MongoDB.Entities.Tests.Models;

public class CustomerWithCustomID : IEntity
{
    [BsonId]
    public string ID { get; set; }

    public object GenerateNewID()
        => $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";

    public bool IsSetID()
      => !string.IsNullOrEmpty(ID);
}
