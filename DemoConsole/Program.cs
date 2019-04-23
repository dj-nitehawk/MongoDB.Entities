using System;
using System.Linq;
using Examples.Models;

using MongoDAL;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //BASIC INITIALIZATION
            //
            ////.Net Core
                new DB("bookshop","localhost",27017);

            ////Asp.Net Core
                //services.AddMongoDAL("DatabaseName", "HostAddress", "PortNumber");

            //ADVANCED INITIALIZATION
            //
            ////.Net Core
                //new DB(new MongoClientSettings()
                //{
                //    Server = new MongoServerAddress("localhost", 27017),
                //    Credential = MongoCredential.CreateCredential("Demo", "username", "password")
                //}, "Demo");

            ////Asp.Net Core
            //services.AddMongoDAL(
            //   new MongoClientSettings()
            //   {
            //       Server = new MongoServerAddress("HostAddress", "PortNumber"),
            //       Credential = MongoCredential.CreateCredential("DatabaseName", "UserName", "Password")
            //   },
            //    "DatabaseName");

            //SAVING
                var book1 = new Book { Title = "The Power Of Now" }; book1.Save();
                var book2 = new Book { Title = "I Am That I Am" }; book2.Save();
                var author1 = new Author { Name = "Eckhart Tolle" }; author1.Save();
                var author2 = new Author { Name = "Nisargadatta Maharaj" }; author2.Save();

            //EMBEDDING CHILDREN
                book1.RelatedAuthor = author2.ToDocument();
                book1.OtherAuthors = (new Author[] { author1, author2 }).ToDocument();

            //RELATIONSHIPS
            //
            /////One-To-One (Embedded)
                book1.RelatedAuthor = author2;

            ////One-To-One (Referenced)
                book1.MainAuthor = author1.ToReference();
                book1.Save();

            ////One-To-Many (Embedded)
                book2.OtherAuthors = new Author[] { author1, author2 };
                book2.Save();

            ////One-To-Many (Referenced)
                book2.Authors.Add(author2); //References are automatically saved. No need to save the entity.      

            //QUERIES
            //
            ////Main collections
                var author = (from a in DB.Collection<Author>()
                              where a.Name.Contains("Eckhart")
                              select a).FirstOrDefault();

            ////Reference collections
                var authors = (from a in book2.Authors.Collection()
                               select a).ToArray();

            ////Get entity of referenced relationship
                var mainAuthor = (from b in DB.Collection<Book>()
                                  where b.Title == book1.Title
                                  select b.MainAuthor)
                                  .SingleOrDefault()
                                  .ToEntity();

            ////Collection shortcut
                var result = from a in author.Collection()
                             select a;

            //DELETE
            //
            ////Delete single entity
                book1.Delete(); //References pointing to this entity are also deleted

            ////Delete multiple entities
                book2.OtherAuthors.DeleteAll();

            ////Delete by lambda expression
                DB.Delete<Book>(b => b.ID == book2.ID);

            //THE END
                Console.WriteLine("Example complete...");

            //todo: readme
        }
    }
}
