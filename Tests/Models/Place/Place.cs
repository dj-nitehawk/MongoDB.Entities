using System;

namespace MongoDB.Entities.Tests;

public abstract class Place : IEntity, IModifiedOn
{
  public string Name { get; set; }
  public Coordinates2D Location { get; set; }
  public double DistanceKM { get; set; }
  public DateTime ModifiedOn { get; set; }
  public abstract object GenerateNewID();
  public abstract bool IsSetID();
}