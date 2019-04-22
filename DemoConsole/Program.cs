using System;
using System.Linq;
using Examples.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDAL;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //INITIALIZE CONNECTION
            new DB("bookshop");

            //SAVING
            var book1 = new Book { Title = "The Power Of Now" }; book1.Save();
            var book2 = new Book { Title = "I Am That I Am" }; book2.Save();

            var author1 = new Author { Name = "Eckhart Tolle" }; author1.Save();
            var author2 = new Author { Name = "Nisargadatta Maharaj" }; author2.Save();

            //RELATIONSHIPS
            //
            //  One-To-One (Embedded)
            book1.RelatedAuthor = author2;

            //  One-To-One (Referenced)
            book1.MainAuthor = author1.ToReference();
            book1.Save();

            //  One-To-Many (Embedded)
            book2.OtherAuthors = new Author[] { author1, author2 };
            book2.Save();

            //  One-To-Many (Referenced)
            book2.Authors.Add(author2); //References are automatically saved. No need to save the entity.      

            //QUERIES
            //
            //  Main collections
            var author = (from a in DB.Collection<Author>()
                          where a.Name.Contains("Eckhart")
                          select a).FirstOrDefault();

            //  Reference collections
            var authors = (from a in book2.Authors.AsQueryable()
                           select a).ToArray();

            //  Get referenced entity
            var mainAuthor = (from b in DB.Collection<Book>()
                              where b.Title == book1.Title
                              select b.MainAuthor)
                              .SingleOrDefault()
                              .ToEntity();

            //DELETE
            //
            //  Delete single entity
            book1.Delete(); //References pointing to this entity are also deleted

            //  Delete multiple entities
            book2.OtherAuthors.DeleteAll();

            //  Delete by lambda expression
            DB.Delete<Book>(b => b.ID == book2.ID);

            //THE END
            Console.WriteLine("CRUD Complete...");

            //todo: test each step + readme
        }
    }
}
