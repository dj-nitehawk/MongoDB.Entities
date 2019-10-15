using System;

namespace MongoDB.Entities.Core
{
    public interface IEntity
    {
        string ID { get; set; }
        DateTime ModifiedOn { get; set; }
    }
}