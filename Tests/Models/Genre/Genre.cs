using System;

namespace MongoDB.Entities.Tests;

public abstract class Genre : IEntity
{
    public string Name { get; set; }
    public Guid GuidID { get; set; }
    public int Position { get; set; }
    public double SortScore { get; set; }
    public Review Review { get; set; }

    public abstract object GenerateNewID();
    public abstract bool HasDefaultID();
}
