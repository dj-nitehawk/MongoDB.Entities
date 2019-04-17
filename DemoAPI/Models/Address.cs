using MongoDAL;

namespace DemoAPI.Models
{
    [MongoIgnoreExtras]
    public class Address : MongoEntity
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }

        public MongoEntity Owner { get; set; }
    }
}
