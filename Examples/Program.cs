using System;
using System.Linq;
using Examples.Models;
using MongoDB.Entities;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            new DB("bookshop", "localhost", 27017);

            //var book = new Book();
            //book.Save();
            //var author1 = new Author { Name = "Tolle" };
            //author1.Save();

            //book.Authors.Add(author1);

            var bk = DB.Collection<Book>().First();
            var res = bk.Authors.Collection().ToArray();

            Console.WriteLine("Example complete...");
        }
    }
}
