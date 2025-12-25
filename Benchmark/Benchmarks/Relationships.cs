using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

namespace Benchmark;

[MemoryDiagnoser]
public class Relationships : BenchBase
{
    const string bookTitle = "BOOKTITLE";
    const string authorName = "AUTHORNAME";

    readonly DB db = DB.Default;

    public Relationships()
    {
        db.Index<Author>()
          .Key(a => a.FirstName!, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync().GetAwaiter().GetResult();

        db.Index<Book>()
          .Key(b => b.Title, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync().GetAwaiter().GetResult();

        db.Index<Book>()
          .Key(b => b.Author.ID, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync().GetAwaiter().GetResult();

        for (var x = 1; x <= 1000; x++)
        {
            var author = new Author
            {
                FirstName = x == 500 ? authorName : "first name " + x,
                LastName = "last name",
                Birthday = DateTime.UtcNow
            };
            db.SaveAsync(author).GetAwaiter().GetResult();

            for (var y = 1; y <= 10; y++)
            {
                var book = new Book
                {
                    Author = author.ToReference(),
                    PublishedOn = DateTime.UtcNow,
                    Title = x == 500 ? bookTitle : "book title " + y
                };
                db.SaveAsync(book).GetAwaiter().GetResult();
                author.Books.AddAsync(book);
            }
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Lookup()
    {
        _ = (await db.Fluent<Author>()
                     .Match(a => a.FirstName == authorName)
                     .Lookup<Author, Book, AuthorWithBooksDTO>(
                         db.Collection<Book>(),
                         a => a.ID,
                         b => b.Author.ID,
                         dto => dto.BookList)
                     .ToListAsync())[0];
    }

    [Benchmark]
    public async Task Clientside_Join()
    {
        var author = await db.Find<Author>().Match(a => a.FirstName == authorName).ExecuteSingleAsync();
        _ = new AuthorWithBooksDTO
        {
            Birthday = author!.Birthday,
            FirstName = author.FirstName,
            LastName = author.LastName,
            ID = author.ID,
            BookList = await db.Find<Book>().ManyAsync(b => Equals(b.Author.ID, author.ID))
        };
    }

    [Benchmark]
    public async Task Children_Fluent()
    {
        var author = await db.Find<Author>().Match(a => a.FirstName == authorName).ExecuteSingleAsync();
        _ = new AuthorWithBooksDTO
        {
            Birthday = author!.Birthday,
            FirstName = author.FirstName,
            LastName = author.LastName,
            ID = author.ID,
            BookList = await author.Books.ChildrenFluent().ToListAsync()
        };
    }

    [Benchmark]
    public async Task Children_Queryable()
    {
        var author = await db.Find<Author>().Match(a => a.FirstName == authorName).ExecuteSingleAsync();
        _ = new AuthorWithBooksDTO
        {
            Birthday = author!.Birthday,
            FirstName = author.FirstName,
            LastName = author.LastName,
            ID = author.ID,
            BookList = await author.Books.ChildrenQueryable().ToListAsync()
        };
    }

    public class AuthorWithBooksDTO : Author
    {
        public List<Book> BookList { get; set; } = new();
    }

    public override Task MongoDB_Entities()
        => throw new NotImplementedException();

    public override Task Official_Driver()
        => throw new NotImplementedException();
}