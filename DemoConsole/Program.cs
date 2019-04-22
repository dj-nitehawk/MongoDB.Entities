using System;
using DemoConsole.Models;
//using MongoDB.Driver;
//using MongoDB.Driver.Linq;
using MongoDAL;
using System.Collections.Generic;
using System.Linq;

namespace DemoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //todo: cleanup examples and readme.md

            //INITIALIZE - Basic
            new DB("Demo");

            //INITIALIZE - Advanced
            //
            //new DB(new MongoClientSettings()
            //{
            //    Server = new MongoServerAddress("localhost", 27017),
            //    Credential = MongoCredential.CreateCredential("Demo", "username", "password")
            //}, "Demo");

            var book1 = new Book { Title = "book 1" };
            book1.SaveChanges();

            var book2 = new Book { Title = "book 2" };
            book2.SaveChanges();

            var author = new Author {
                Name = "person 1",
                BestSeller = book1.ToReference()
            };

            author.SaveChanges();
            author.Books.Add(book1);
            author.Books.Add(book2);

            author.Books.Remove(book2);
            
            Console.WriteLine("CRUD Complete...");
        }
    }
}
