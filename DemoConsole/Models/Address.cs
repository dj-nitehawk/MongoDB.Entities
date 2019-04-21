using MongoDAL;

namespace DemoConsole.Models
{
    public class Address : Entity
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public RefOne<Person> Owner { get; set; }

        //public void Save()
        //{
        //    DB.Save<Address>(this);
        //}

        public void DeleteByOwnerId(string ownerID)
        {
            DB.Delete<Address>(a => a.Owner.ID.Equals(ownerID));
        }
    }
}
