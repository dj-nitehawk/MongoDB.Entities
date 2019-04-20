using MongoDAL;

namespace DemoAPI.Models
{
    public class Address : Entity
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }

        [Reference]
        public string OwnerId { get; set; }

        public void Save()
        {
            DB.Save<Address>(this);
        }

        public void DeleteByOwnerId(string ownerID)
        {
            DB.Delete<Address>(a => a.OwnerId.Equals(ownerID));
        }
    }
}
