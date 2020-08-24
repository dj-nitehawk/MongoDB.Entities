using Examples.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Examples
{
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            //BASIC INITIALIZATION
            //
            await DB.InitAsync("bookshop", "localhost", 27017);

            //ADVANCED INITIALIZATION
            //
            //await DB.InitAsync(new MongoClientSettings()
            //{
            //    Server = new MongoServerAddress("localhost", 27017),
            //    Credential = MongoCredential.CreateCredential("Demo", "username", "password")
            //}, "Demo");

            var stopWatch = new Stopwatch(); stopWatch.Start();

            //SAVING
            var book1 = new Book { Title = "The Power Of Now" };
            var book2 = new Book { Title = "I Am That I Am" };
            var author1 = new Author { Name = "Eckhart Tolle" };
            var author2 = new Author { Name = "Nisargadatta Maharaj" };
            var genre1 = new Genre { Name = "Self Help" };

            await new[] { book1, book2 }.SaveAsync();
            await new[] { author1, author2 }.SaveAsync();
            await genre1.SaveAsync();

            //EMBEDDING DOCUMENTS
            book1.Review = new Review { Stars = 5, Reviewer = "New York Times" }; //Review does not inherit from Entity.
            book1.RelatedAuthor = author2; //alt: author2.ToDocument();
            book1.OtherAuthors = new[] { author1, author2 }; //alt: new[] { author1, author2 }.ToDocuments();
            await book1.SaveAsync();

            //RELATIONSHIPS
            //
            /////One-To-One (Embedded)
            book1.RelatedAuthor = author2;

            ////One-To-One (Referenced)
            book1.MainAuthor = author1; //alt: author1.ToReference();
            await book1.SaveAsync();

            ////One-To-Many (Embedded)
            book2.OtherAuthors = new Author[] { author1, author2 };
            await book2.SaveAsync();

            ////One-To-Many (Referenced)
            await book2.Authors.AddAsync(new[] { author1, author2 }); //References are automatically saved. No need to save the entity.      

            ////Many-To-Many (Referenced)
            await genre1.AllBooks.AddAsync(new[] { book1, book2 });

            //QUERIES
            //
            ////Main collections
            var author = await (from a in DB.Queryable<Author>()
                                where a.Name.Contains("Eckhart")
                                select a).FirstOrDefaultAsync();

            ////Reference collections
            var authors = await (from a in book2.Authors.ChildrenQueryable()
                                 select a).ToListAsync();

            ////Get entity of referenced relationship
            var mainAuthor = await (
                                await (from b in DB.Queryable<Book>()
                                       where b.Title == book1.Title
                                       select b.MainAuthor)
                                       .SingleOrDefaultAsync())
                                .ToEntityAsync();

            ////Collection shortcut
            var result = from a in author.Queryable()
                         select a;

            //DELETE
            //
            ////Delete single entity
            await book1.RelatedAuthor.DeleteAsync();
            book1.RelatedAuthor = null;
            await book1.SaveAsync();
            await book1.DeleteAsync(); //References pointing to this entity are also deleted

            ////Delete multiple entities
            await book2.OtherAuthors.DeleteAllAsync();
            book2.OtherAuthors = null;
            await book2.SaveAsync();

            ////Delete by lambda expression
            await DB.DeleteAsync<Book>(b => b.ID == book2.ID);

            //THE END
            Console.WriteLine($"All operations completed in {stopWatch.Elapsed.TotalSeconds:0.00} seconds.");
            Console.ReadLine();
        }
    }
}
