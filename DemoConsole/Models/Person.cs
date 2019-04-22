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
        public One<Address> HomeAddress { get; set; }
        public Many<Person, Address> AllAddresses { get; set; }

        public Person()
        {
            AllAddresses = AllAddresses.Initialize(this);
        }

        public Person FindLast()
        {
            return (from p in DB.Collection<Person>()
                    orderby p.ModifiedOn descending
                    select p).FirstOrDefault();
        }

    }
}
