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



            Console.WriteLine("CRUD Complete...");
        }
    }
}
