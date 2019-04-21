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

            //CREATE
            var person = new Person
            {
                Name = "Test " + DateTime.UtcNow.Ticks.ToString(),
                Age = 32,
                PhoneNumbers = new string[] { "123456", "654321", "555555" },
                RetirementDate = DateTime.UtcNow
            };

            person.SaveToDB();

            //CREATE Async
            //await DB.SaveAsync<Person>(person);

            var ad1 = new Address
            {
                Line1 = "add 1",
                Line2 = person.ID,
                Owner = person.ToReference()
            };

            ad1.SaveToDB();

            person.HomeAddress = ad1.ToReference();

            var ad2 = new Address() { Line1 = "add 2", Line2 = person.ID, Owner = person.ToReference() };
            ad2.SaveToDB();

            person.AllAddresses.Add(ad1);
            person.AllAddresses.Add(ad2);

            var q = person.AllAddresses.Collection.OrderByDescending(a => a.ModifiedOn).FirstOrDefault();

            //person.AllAddresses.Remove(address);


            person.SaveToDB();


            //READ
            var lastPerson = person.FindLast();

            lastPerson.SaveToDB();

            var x = await person.HomeAddress.ToEntityAsync();

            //READ Async
            //var lastPerson = await (from p in DB.Collection<Person>()
            //                        orderby p.ModifiedOn descending
            //                        select p).FirstOrDefaultAsync();

            //UPDATE
            lastPerson.Name = "Updated at " + DateTime.UtcNow.ToString();
            lastPerson.SaveToDB();

            //DELETE
            //lastPerson.Delete();
            //address.DeleteByOwnerId(lastPerson.Id);   

            //DELETE Async
            //await DB.DeleteAsync<Person>(lastPerson.Id);

            Console.WriteLine("CRUD Complete...");
        }
    }
}
