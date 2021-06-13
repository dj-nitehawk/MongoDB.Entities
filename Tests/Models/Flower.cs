using System;

namespace MongoDB.Entities.Tests.Models
{
    public class Flower : Entity
    {
        public DateTime CreatedDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public Many<CustomerWithCustomID> Customers { get; set; }

        public Flower()
        {
            this.InitOneToMany(() => Customers);
        }
    }
}
