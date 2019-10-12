using MongoDB.Entities.Common;

namespace MongoDB.Entities.Tests
{
    public class Place : Entity
    {
        public string Name { get; set; }
        public Coordinates2D Location { get; set; }
        public double DistanceKM { get; set; }
    }
}
