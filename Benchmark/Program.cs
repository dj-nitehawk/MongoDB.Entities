using MongoDB.Entities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmark
{
    public class Author : Entity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Date Birthday { get; set; }
        public Many<Book> Books { get; set; }

        public Author() => this.InitOneToMany(() => Books);
    }

    public class Book : Entity
    {
        public string Title { get; set; }
        public One<Author> Author { get; set; }
        public Date PublishedOn { get; set; }
    }

    public static class Program
    {
        private static readonly ConcurrentBag<byte> booksCreated = new ConcurrentBag<byte>();
        private static readonly ConcurrentBag<byte> authorsCreated = new ConcurrentBag<byte>();
        private const int authorCount = 1000;
        private const int booksPerAuthor = 1000;
        private const int concurrentTasks = 32;

        private static async Task Main()
        {
            await DB.InitAsync("benchmark-mongodb-entities");

            Console.WriteLine("creating 1 million books and 1000 authors...");
            Console.WriteLine();

            var sw = new Stopwatch();
            sw.Start();

            var range = Enumerable.Range(1, authorCount);

            Parallel.ForEach(range, new ParallelOptions { MaxDegreeOfParallelism = concurrentTasks }, async number =>
            {
                var author = new Author
                {
                    FirstName = "first name " + number.ToString(),
                    LastName = "last name " + number.ToString(),
                    Birthday = DateTime.UtcNow
                };
                await author.SaveAsync();
                authorsCreated.Add(0);

                var book = new Book();

                for (int i = 1; i <= booksPerAuthor; i++)
                {
                    book.ID = null;
                    book.Title = $"author {number} - book {i}";
                    book.PublishedOn = DateTime.UtcNow;
                    book.Author = author.ID;
                    await book.SaveAsync();
                    await author.Books.AddAsync(book);
                    booksCreated.Add(0);

                    Console.Write($"\rauthors: {authorsCreated.Count} | books: {booksCreated.Count}                    ");

                }
            });

            Console.WriteLine();
            Console.WriteLine($"done in {sw.Elapsed:hh':'mm':'ss}");
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            sw.Restart();
            var author = (await DB.Find<Author>()
                           .Match(a => a.FirstName == "first name 666" && a.LastName == "last name 666")
                           .ExecuteAsync())
                           .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine($"found author 666 by name in [{sw.Elapsed.TotalMilliseconds:0}ms] with an un-indexed query - his id: {author.ID}");
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            sw.Restart();
            author = await DB.Find<Author>()
                       .OneAsync(author.ID);

            Console.WriteLine();
            Console.WriteLine($"looking up author 666 by ID took [{sw.Elapsed.TotalMilliseconds:0}ms]");
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            sw.Restart();
            var book555 = author.Books
                            .ChildrenQueryable()
                            .Where(b => b.Title == "author 666 - book 555")
                            .ToList()
                            .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine($"found book 555 of author 666 by title in [{sw.Elapsed.TotalMilliseconds:0}ms] - title field is not indexed");
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine("creating index for book title...");
            sw.Restart();
            var indexTask = DB.Index<Book>()
                              .Key(b => b.Title, KeyType.Ascending)
                              .Option(o => o.Background = false)
                              .CreateAsync();

            while (!indexTask.IsCompleted)
            {
                Console.Write($"\rindexing time: {sw.Elapsed.TotalSeconds:0} seconds");
                Task.Delay(1000).Wait();
            }
            Console.WriteLine();
            Console.WriteLine("indexing done!");
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            sw.Restart();
            book555 = author.Books
                            .ChildrenQueryable()
                            .Where(b => b.Title == "author 666 - book 555")
                            .ToList()
                            .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine($"found book 555 of author 666 by title in [{sw.Elapsed.TotalMilliseconds:0}ms] - title field is indexed");
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            sw.Restart();
            var bookIDs = await DB.Find<Book, string>()
                            .Match(b => b.Title == "author 999 - book 999" ||
                                        b.Title == "author 333 - book 333")
                            .Project(b => b.ID)
                            .ExecuteAsync();

            Console.WriteLine();
            Console.WriteLine($"fetched 2 book IDs by title in [{sw.Elapsed.TotalMilliseconds:0}ms] - title field is indexed");
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            sw.Restart();
            var parents = DB.Entity<Author>().Books
                            .ParentsQueryable<Author>(bookIDs)
                            .ToArray();

            Console.WriteLine();
            Console.WriteLine($"reverse relationship access finished in [{sw.Elapsed.TotalMilliseconds:0}ms]");
            Console.WriteLine();
            Console.WriteLine("the following authors were returned:");
            Console.WriteLine();
            foreach (var a in parents)
            {
                Console.WriteLine($"name: {a.FirstName} {a.LastName}");
            }
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            _ = DB.Collection<Book>().Indexes.DropAllAsync();
        }
    }
}
