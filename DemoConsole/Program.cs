using System;
using DemoConsole.Models;
//using MongoDB.Driver;

namespace DemoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //INITIALIZE - Basic
            new MongoDAL.DB("Demo");

            //INITIALIZE - Advanced
            //
            //new MongoDAL.DB(new MongoClientSettings()
            //{
            //    Server = new MongoServerAddress("localhost", 27017),
            //    Credential = MongoCredential.CreateCredential("Demo", "username", "password")
            //}, "Demo");

            //CREATE
            var person = new Person
            {
                Name = "Test " + DateTime.UtcNow.Ticks.ToString(),
                Age = 32,
                PhoneNumbers = new string[] { "123456", "654321", "555555" },
                RetirementDate = DateTime.UtcNow
            };

            person.Save();

            var address = new Address
            {
                Line1 = "line 1",
                City = "Colarado",
                OwnerId = person.Id
            };

            address.Save();

            //READ
            var lastPerson = person.FindLast();

            //UPDATE
            lastPerson.Name = "Updated at " + DateTime.UtcNow.ToString();
            lastPerson.Save();

            //DELETE
            //lastPerson.Delete();
            //address.DeleteByOwnerId(lastPerson.Id);   

            Console.WriteLine("CRUD Complete...");
            Console.ReadKey();
        }
    }
}
