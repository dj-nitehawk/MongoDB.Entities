using MongoDB.Entities.Core;

namespace MongoDB.Entities.Tests.Models
{
    public class Flower : Entity
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }
}
