using System;

namespace MongoDB.Entities.Tests.Models
{
    public class CustomIDOverride : Entity
    {
        public override string GenerateNewID()
            => DateTime.UtcNow.Ticks.ToString();
    }
}
