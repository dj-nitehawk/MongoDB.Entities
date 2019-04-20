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
        public Reference<Address> HomeAddress { get; set; }
        //public ReferenceCollection<Address> AllAddresses { get; set; }
        public Collection<Reference<Address>> AllAddresses { get; set; }

        //public void Save()
        //{
        //    DB.Save<Person>(this);
        //}

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
