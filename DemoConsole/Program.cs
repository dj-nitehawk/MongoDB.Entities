using System;
using DemoConsole.Models;
using System.Threading.Tasks;
//using MongoDB.Driver;
//using MongoDB.Driver.Linq;
using MongoDAL;
using System.Collections.Generic;
using System.Linq;

namespace DemoConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //todo: cleanup examples and readme.md

            //INITIALIZE - Basic
            new MongoDAL.DB("Demo");

            //INITIALIZE - Advanced
            //
            //new MongoDAL.DB(new MongoClientSettings()
            //{
            //    Server = new MongoServerAddress("localhost", 27017),
            //    Credential = MongoCredential.CreateCredential("Demo", "username", "password")
            //}, "Demo");

            var ad1 = new Address { Line1 = "address 1" };
            ad1.SaveChanges();

            var ad2 = new Address { Line1 = "address 2" };
            ad2.SaveChanges();

            var person = new Person {
                Name = "person 1",
                HomeAddress = ad1.ToReference()
            };

            person.SaveChanges();
            person.AllAddresses.Add(ad1);
            person.AllAddresses.Add(ad2);

            person.AllAddresses.Remove(ad2);
            
            Console.WriteLine("CRUD Complete...");
        }
    }
}
