using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class Relationships : BenchBase
{
    const string bookTitle = "BOOKTITLE";
    const string authorName = "AUTHORNAME";

    public Relationships()
    {
        DB.Index<Author>()
          .Key(a => a.FirstName!, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync().GetAwaiter().GetResult();

        DB.Index<Book>()
          .Key(b => b.Title, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync().GetAwaiter().GetResult();

        DB.Index<Book>()
          .Key(b => b.Author.ID, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync().GetAwaiter().GetResult();

        for (int x = 1; x <= 1000; x++)
        {
            var author = new Author
            {
                FirstName = x == 500 ? authorName : "first name " + x,
                LastName = "last name",
                Birthday = DateTime.UtcNow
            };
            author.SaveAsync().GetAwaiter().GetResult();

            for (int y = 1; y <= 10; y++)
            {
                var book = new Book
                {
                    Author = author.ToReference(),
                    PublishedOn = DateTime.UtcNow,
                    Title = x == 500 ? bookTitle : "book title " + y
                };
                book.SaveAsync().GetAwaiter().GetResult();
                author.Books.AddAsync(book);
            }
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Lookup()
    {
        var res = (await DB
            .Fluent<Author>()
            .Match(a => a.FirstName == authorName)
            .Lookup<Author, Book, AuthorWithBooksDTO>(
                DB.Collection<Book>(),
                a => a.ID,
                b => b.Author.ID,
                dto => dto.BookList)
            .ToListAsync())[0];
    }

    [Benchmark]
    public async Task Clientside_Join()
    {
        var author = await DB.Find<Author>().Match(a => a.FirstName == authorName).ExecuteSingleAsync();
        var res = new AuthorWithBooksDTO
        {
            Birthday = author!.Birthday,
            FirstName = author.FirstName,
            LastName = author.LastName,
            ID = author.ID,
            BookList = await DB.Find<Book>().ManyAsync(b => Equals(b.Author.ID, author != null ? author.ID : null))
        };
    }

    [Benchmark]
    public async Task Children_Fluent()
    {
        var author = await DB.Find<Author>().Match(a => a.FirstName == authorName).ExecuteSingleAsync();
        var res = new AuthorWithBooksDTO
        {
            Birthday = author!.Birthday,
            FirstName = author.FirstName,
            LastName = author.LastName,
            ID = author.ID,
            BookList = author is null ? new List<Book>() : await author.Books.ChildrenFluent().ToListAsync()
        };
    }

    [Benchmark]
    public async Task Children_Queryable()
    {
        var author = await DB.Find<Author>().Match(a => a.FirstName == authorName).ExecuteSingleAsync();
        var res = new AuthorWithBooksDTO
        {
            Birthday = author!.Birthday,
            FirstName = author.FirstName,
            LastName = author.LastName,
            ID = author.ID,
            BookList = author is null ? new List<Book>() : await author.Books.ChildrenQueryable().ToListAsync()
        };
    }

    public class AuthorWithBooksDTO : Author
    {
        public List<Book> BookList { get; set; } = new();
    }

    public override Task MongoDB_Entities()
    {
        throw new NotImplementedException();
    }

    public override Task Official_Driver()
    {
        throw new NotImplementedException();
    }
}
