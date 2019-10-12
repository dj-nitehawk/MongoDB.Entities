using System;

namespace MongoDB.Entities.Common
{
    public interface IEntity
    {
        string ID { get; set; }
        DateTime ModifiedOn { get; set; }
    }
}