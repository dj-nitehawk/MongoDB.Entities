using System;
using MongoDAL;
using System.Linq;
using System.Collections.ObjectModel;

namespace DemoConsole.Models

{
    public class Person : Entity
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] PhoneNumbers { get; set; }
        public DateTime? RetirementDate { get; set; }
        public RefOne<Address> HomeAddress { get; set; }
        public RefMany<Person,Address> AllAddresses { get; set; }

        public void Save()
        {
            //this.SaveToDB();
        }

        public Person FindLast()
        {
            return (from p in DB.Collection<Person>()
                    orderby p.ModifiedOn descending
                    select p).FirstOrDefault();
        }

        public void Delete()
        {
            DB.Delete<Person>(this.ID);
        }

    }
}
