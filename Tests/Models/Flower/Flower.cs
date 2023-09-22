using System;

namespace MongoDB.Entities.Tests.Models;

public abstract class BaseEntity : IEntity
{
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime UpdateDate { get; set; }
    public string UpdatedBy { get; set; }
    public abstract object GenerateNewID();
    public abstract bool HasDefaultID();
}

public abstract class Flower : BaseEntity, ISoftDeleted
{
    public string Name { get; set; }
    public string Color { get; set; }
    public bool IsDeleted { get; set; }
}

public interface ISoftDeleted
{
    public bool IsDeleted { get; set; }
}
