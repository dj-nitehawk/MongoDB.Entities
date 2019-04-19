using System;
using DemoConsole.Models;
using System.Threading.Tasks;
//using MongoDB.Driver;
//using MongoDB.Driver.Linq;
//using MongoDAL;

namespace DemoConsole
{
    class Program
    {
        static async Task Main(string[] args)
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

            //CREATE Async
            //await DB.SaveAsync<Person>(person);

            var address = new Address
            {
                Line1 = "line 1",
                City = "Colarado",
                OwnerId = person.Id
            };

            address.Save();

            //READ
            var lastPerson = person.FindLast();

            //READ Async
            //var lastPerson = await DB.Collection<Person>()
            //    .OrderByDescending(p=> p.ModifiedOn)
            //    .FirstOrDefaultAsync();

            //UPDATE
            lastPerson.Name = "Updated at " + DateTime.UtcNow.ToString();
            lastPerson.Save();

            //DELETE
            //lastPerson.Delete();
            //address.DeleteByOwnerId(lastPerson.Id);   

            //DELETE Async
            //await DB.DeleteAsync<Person>(lastPerson.Id);

            Console.WriteLine("CRUD Complete...");
        }
    }
}
