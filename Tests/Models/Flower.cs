using System;

namespace MongoDB.Entities.Tests.Models
{
    public class BaseEntity : Entity
    {
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class Flower : BaseEntity
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public Many<CustomerWithCustomID> Customers { get; set; }

        public Flower()
        {
            this.InitOneToMany(() => Customers);
        }
    }
}
