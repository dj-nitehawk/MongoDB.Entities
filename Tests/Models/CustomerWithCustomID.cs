using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.Tests.Models;

public class CustomerWithCustomID : IEntity
{
    [BsonId]
    public string ID { get; set; }
}
