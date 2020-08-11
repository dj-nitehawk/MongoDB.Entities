using System;

namespace MongoDB.Entities.Tests
{
    public class Place : Entity, IModifiedOn
    {
        public string Name { get; set; }
        public Coordinates2D Location { get; set; }
        public double DistanceKM { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
