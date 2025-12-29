using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class RelationshipsEntity
{
    readonly DB _db = DB.Default;

    [TestMethod]
    public async Task setting_one_to_one_reference_returns_correct_entity()
    {
        var book = new BookEntity { Title = "book" };
        var author = new AuthorEntity { Name = "sotorrce" };
        await _db.SaveAsync(author);
        book.MainAuthor = author.ToReference();
        await _db.SaveAsync(book);
        var res = await (await _db.Queryable<BookEntity>()
                                  .Where(b => b.ID == book.ID)
                                  .SingleAsync())
                        .MainAuthor.ToEntityAsync(_db);
        Assert.AreEqual(author.Name, res.Name);
    }

    [TestMethod]
    public async Task setting_one_to_one_reference_with_implicit_operator_by_string_id_returns_correct_entity()
    {
        var book = new BookEntity { Title = "book" };
        var author = new AuthorEntity { Name = "sotorrce" };
        await _db.SaveAsync(author);
        book.MainAuthor = author.ToReference();
        await _db.SaveAsync(book);
        var res = await (await _db.Queryable<BookEntity>()
                                  .Where(b => b.ID == book.ID)
                                  .SingleAsync())
                        .MainAuthor.ToEntityAsync(_db);
        Assert.AreEqual(author.Name, res.Name);
    }

    [TestMethod]
    public async Task setting_one_to_one_reference_with_implicit_operator_by_entity_returns_correct_entity()
    {
        var book = new BookEntity { Title = "book" };
        var author = new AuthorEntity { Name = "soorfioberce" };
        await _db.SaveAsync(author);
        book.MainAuthor = author.ToReference();
        await _db.SaveAsync(book);
        var res = await (await _db.Queryable<BookEntity>()
                                  .Where(b => b.ID == book.ID)
                                  .SingleAsync())
                        .MainAuthor.ToEntityAsync(_db);
        Assert.AreEqual(author.Name, res.Name);
    }

    [TestMethod]
    public async Task one_to_one_to_entity_with_lambda_projection()
    {
        var book = new BookEntity { Title = "book" };
        var author = new AuthorEntity { Name = "ototoewlp" };
        await _db.SaveAsync(author);
        book.MainAuthor = author.ToReference();
        await _db.SaveAsync(book);
        var res = await (await _db.Queryable<BookEntity>()
                                  .Where(b => b.ID == book.ID)
                                  .SingleAsync()).MainAuthor.ToEntityAsync(a => new() { Name = a.Name }, _db);

        Assert.AreEqual(author.Name, res.Name);
        Assert.IsNull(res.ID);
    }

    [TestMethod]
    public async Task one_to_one_to_entity_with_mongo_projection()
    {
        var book = new BookEntity { Title = "book" };
        var author = new AuthorEntity { Name = "ototoewmp" };
        await _db.SaveAsync(author);
        book.MainAuthor = author.ToReference();
        await _db.SaveAsync(book);
        var res = await (await _db.Queryable<BookEntity>()
                                  .Where(b => b.ID == book.ID)
                                  .SingleAsync()).MainAuthor.ToEntityAsync(p => p.Include(a => a.Name).Exclude(a => a.ID), _db);
        Assert.AreEqual(author.Name, res.Name);
        Assert.IsNull(res.ID);
    }

    [TestMethod]
    public async Task adding_one2many_references_returns_correct_entities_queryable()
    {
        var author = new AuthorEntity { Name = "author" };
        var book1 = new BookEntity { Title = "aotmrrceb1" };
        var book2 = new BookEntity { Title = "aotmrrceb2" };
        await _db.SaveAsync(book1);
        await _db.SaveAsync(book2);
        await _db.SaveAsync(author);
        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);
        var books = await _db.Queryable<AuthorEntity>()
                             .Where(a => a.ID == author.ID)
                             .Single()
                             .Books
                             .ChildrenQueryable().ToListAsync();
        Assert.AreEqual(book2.Title, books[1].Title);
    }

    [TestMethod]
    public async Task ienumerable_for_many()
    {
        var author = new AuthorEntity { Name = "author" };
        var book1 = new BookEntity { Title = "aotmrrceb1" };
        var book2 = new BookEntity { Title = "aotmrrceb2" };
        await _db.SaveAsync(book1);
        await _db.SaveAsync(book2);
        await _db.SaveAsync(author);
        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);
        var books = (await _db.Queryable<AuthorEntity>()
                              .Where(a => a.ID == author.ID)
                              .SingleAsync())
            .Books;

        List<BookEntity> booklist = [];

        booklist.AddRange(books);

        Assert.HasCount(2, booklist);
    }

    [TestMethod]
    public async Task adding_one2many_references_returns_correct_entities_fluent()
    {
        var author = new AuthorEntity { Name = "author" };
        var book1 = new BookEntity { Title = "aotmrrcebf1" };
        var book2 = new BookEntity { Title = "aotmrrcebf2" };
        await _db.SaveAsync(book1);
        await _db.SaveAsync(book2);
        await _db.SaveAsync(author);
        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);
        var books = await _db.Queryable<AuthorEntity>()
                             .Where(a => a.ID == author.ID)
                             .Single()
                             .Books
                             .ChildrenFluent().ToListAsync();
        Assert.AreEqual(book2.Title, books[1].Title);
    }

    [TestMethod]
    public async Task removing_a_one2many_ref_removes_correct_entities()
    {
        var book = new BookEntity { Title = "rotmrrceb" };
        await _db.SaveAsync(book);
        var author1 = new AuthorEntity { Name = "rotmrrcea1" };
        await _db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "rotmrrcea2" };
        await _db.SaveAsync(author2);
        await book.GoodAuthors.AddAsync(author1);
        await book.GoodAuthors.AddAsync(author2);
        var remAuthor = await DB.Default.Queryable<AuthorEntity>()
                                .Where(a => a.ID == author2.ID)
                                .SingleAsync();
        await book.GoodAuthors.RemoveAsync(remAuthor);
        var resBook = await _db.Queryable<BookEntity>()
                               .Where(b => b.ID == book.ID)
                               .SingleAsync();
        Assert.AreEqual(1, await resBook.GoodAuthors.ChildrenQueryable().CountAsync());
        Assert.AreEqual(author1.Name, (await resBook.GoodAuthors.ChildrenQueryable().FirstAsync()).Name);
    }

    [TestMethod]
    public async Task collection_shortcut_of_many_returns_correct_children()
    {
        var book = new BookEntity { Title = "book" };
        await _db.SaveAsync(book);
        var author1 = new AuthorEntity { Name = "csomrcc1" };
        await _db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "csomrcc1" };
        await _db.SaveAsync(author2);
        await book.GoodAuthors.AddAsync(author1);
        await book.GoodAuthors.AddAsync(author2);
        Assert.AreEqual(2, await book.GoodAuthors.ChildrenQueryable().CountAsync());
        Assert.AreEqual(author1.Name, (await book.GoodAuthors.ChildrenQueryable().FirstAsync()).Name);
    }

    [TestMethod]
    public void accessing_coll_shortcut_on_unsaved_parent_throws()
    {
        var book = new BookEntity { Title = "acsoupt" };
        Assert.ThrowsExactly<InvalidOperationException>(() => book.GoodAuthors.ChildrenQueryable().Count());
    }

    [TestMethod]
    public async Task many_children_count()
    {
        var book1 = new BookEntity { Title = "mcc" };
        await _db.SaveAsync(book1);
        var gen1 = new GenreEntity { Name = "ac2mrceg1" };
        await _db.SaveAsync(gen1);
        var gen2 = new GenreEntity { Name = "ac2mrceg1" };
        await _db.SaveAsync(gen2);

        await book1.Genres.AddAsync(gen1);
        await book1.Genres.AddAsync(gen2);

        Assert.AreEqual(2, await book1.Genres.ChildrenCountAsync());

        var book2 = new BookEntity { Title = "mcc" };
        await _db.SaveAsync(book2);

        await gen1.Books.AddAsync(book1);
        await gen1.Books.AddAsync(book2);

        Assert.AreEqual(2, await gen1.Books.ChildrenCountAsync());
    }

    [TestMethod]
    public async Task adding_many2many_returns_correct_children()
    {
        var book1 = new BookEntity { Title = "ac2mrceb1" };
        await _db.SaveAsync(book1);
        var book2 = new BookEntity { Title = "ac2mrceb2" };
        await _db.SaveAsync(book2);

        var gen1 = new GenreEntity { Name = "ac2mrceg1" };
        await _db.SaveAsync(gen1);
        var gen2 = new GenreEntity { Name = "ac2mrceg1" };
        await _db.SaveAsync(gen2);

        await book1.Genres.AddAsync(gen1);
        await book1.Genres.AddAsync(gen2);
        await book1.Genres.AddAsync(gen1);
        Assert.AreEqual(2, DB.Default.Queryable<BookEntity>().Where(b => b.ID == book1.ID).Single().Genres.ChildrenQueryable().Count());
        Assert.AreEqual(gen1.Name, book1.Genres.ChildrenQueryable().First().Name);

        await gen1.Books.AddAsync(book1);
        await gen1.Books.AddAsync(book2);
        await gen1.Books.AddAsync(book1);
        Assert.AreEqual(2, _db.Queryable<GenreEntity>().Where(g => g.ID == gen1.ID).Single().Books.ChildrenQueryable().Count());
        Assert.AreEqual(gen1.Name, _db.Queryable<BookEntity>().Where(b => b.ID == book2.ID).Single().Genres.ChildrenQueryable().First().Name);

        await gen2.Books.AddAsync(book1);
        await gen2.Books.AddAsync(book2);
        Assert.AreEqual(2, book1.Genres.ChildrenQueryable().Count());
        Assert.AreEqual(gen2.Name, _db.Queryable<BookEntity>().Where(b => b.ID == book2.ID).Single().Genres.ChildrenQueryable().First().Name);
    }

    [TestMethod]
    public async Task removing_many2many_returns_correct_children()
    {
        var book1 = new BookEntity { Title = "rm2mrceb1" };
        await _db.SaveAsync(book1);
        var book2 = new BookEntity { Title = "rm2mrceb2" };
        await _db.SaveAsync(book2);

        var gen1 = new GenreEntity { Name = "rm2mrceg1" };
        await _db.SaveAsync(gen1);
        var gen2 = new GenreEntity { Name = "rm2mrceg1" };
        await _db.SaveAsync(gen2);

        await book1.Genres.AddAsync(gen1);
        await book1.Genres.AddAsync(gen2);
        await book2.Genres.AddAsync(gen1);
        await book2.Genres.AddAsync(gen2);

        await book1.Genres.RemoveAsync(gen1);
        Assert.AreEqual(1, await book1.Genres.ChildrenQueryable().CountAsync());
        Assert.AreEqual(gen2.Name, (await book1.Genres.ChildrenQueryable().SingleAsync()).Name);
        Assert.AreEqual(1, await gen1.Books.ChildrenQueryable().CountAsync());
        Assert.AreEqual(book2.Title, (await gen1.Books.ChildrenQueryable().FirstAsync()).Title);
    }

    [TestMethod]
    public async Task getting_parents_of_a_relationship_fluent_works()
    {
        var guid = Guid.NewGuid().ToString();

        var book = new BookEntity { Title = "Planet Of The Apes " + guid };
        await _db.SaveAsync(book);

        var genre = new GenreEntity { Name = "SciFi " + guid };
        await _db.SaveAsync(genre);

        var genre1 = new GenreEntity { Name = "Thriller " + guid };
        await _db.SaveAsync(genre1);

        await book.Genres.AddAsync(genre);
        await book.Genres.AddAsync(genre1);

        var books = await book.Genres
                              .ParentsFluent(genre.ID)
                              .ToListAsync();

        Assert.HasCount(1, books);
        Assert.AreEqual(book.Title, books.Single().Title);

        books = await book.Genres
                          .ParentsFluent(_db.Fluent<GenreEntity>().Match(g => g.Name.Contains(guid)))
                          .ToListAsync();

        Assert.HasCount(1, books);
        Assert.AreEqual(book.Title, books.Single().Title);

        var genres = await genre.Books
                                .ParentsFluent([book.ID])
                                .ToListAsync();

        Assert.HasCount(2, genres);
        Assert.AreEqual(genre.Name, genres.Single(g => g.ID == genre.ID).Name);

        _ = await genre.Books
                       .ParentsFluent(_db.Fluent<BookEntity>().Match(b => b.ID == book.ID))
                       .ToListAsync();

        Assert.HasCount(1, books);
        Assert.AreEqual(book.Title, books.Single().Title);
    }

    [TestMethod]
    public async Task add_child_to_many_relationship_with_ID()
    {
        var author = new AuthorEntity { Name = "author" };
        await _db.SaveAsync(author);

        var b1 = new BookEntity { Title = "book1" };
        await _db.SaveAsync(b1);
        var b2 = new BookEntity { Title = "book2" };
        await _db.SaveAsync(b2);

        await author.Books.AddAsync(b1.ID);
        await author.Books.AddAsync(b2.ID);

        var books = await author.Books
                                .ChildrenQueryable()
                                .OrderBy(b => b.Title)
                                .ToListAsync();

        Assert.HasCount(2, books);
        Assert.AreEqual("book1", books[0].Title);
        Assert.AreEqual("book2", books[1].Title);
    }

    [TestMethod]
    public async Task relationships_with_custom_ID()
    {
        var customer = new CustomerWithCustomID();
        await _db.SaveAsync(customer);

        var flower = new FlowerEntity { Name = customer.ID };
        await _db.SaveAsync(flower);

        var flower2 = new FlowerEntity();
        await _db.SaveAsync(flower2);

        await flower.Customers.AddAsync(customer);

        var cust = await flower.Customers
                               .ChildrenQueryable()
                               .Where(c => c.ID == customer.ID)
                               .SingleAsync();

        Assert.AreEqual(cust.ID, customer.ID);
    }

    [TestMethod]
    public async Task remove_child_from_many_relationship_with_ID()
    {
        var author = new AuthorEntity { Name = "author" };
        await _db.SaveAsync(author);

        var b1 = new BookEntity { Title = "book1" };
        await _db.SaveAsync(b1);
        var b2 = new BookEntity { Title = "book2" };
        await _db.SaveAsync(b2);

        await author.Books.AddAsync(b1.ID);
        await author.Books.AddAsync(b2.ID);

        await author.Books.RemoveAsync(b1.ID);
        await author.Books.RemoveAsync(b2.ID);

        var count = await author.Books
                                .ChildrenQueryable()
                                .CountAsync();

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task overload_operator_for_adding_children_to_many_relationships()
    {
        var author = new AuthorEntity { Name = "author" };
        await _db.SaveAsync(author);

        var b1 = new BookEntity { Title = "book1" };
        await _db.SaveAsync(b1);
        var b2 = new BookEntity { Title = "book2" };
        await _db.SaveAsync(b2);

        await author.Books.AddAsync(b1);
        await author.Books.AddAsync(b2.ID);

        var books = await author.Books
                                .ChildrenQueryable()
                                .OrderBy(b => b.Title)
                                .ToListAsync();

        Assert.HasCount(2, books);
        Assert.AreEqual("book1", books[0].Title);
        Assert.AreEqual("book2", books[1].Title);
    }

    [TestMethod]
    public async Task overload_operator_for_removing_children_from_many_relationships()
    {
        var author = new AuthorEntity { Name = "author" };
        await _db.SaveAsync(author);

        var b1 = new BookEntity { Title = "book1" };
        await _db.SaveAsync(b1);
        var b2 = new BookEntity { Title = "book2" };
        await _db.SaveAsync(b2);

        await author.Books.AddAsync(b1);
        await author.Books.AddAsync(b2.ID);

        await author.Books.RemoveAsync(b1);
        await author.Books.RemoveAsync(b2.ID);

        var count = await author.Books
                                .ChildrenQueryable()
                                .CountAsync();

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task many_to_many_remove_multiple()
    {
        var a1 = new AuthorEntity { Name = "author one" };
        var a2 = new AuthorEntity { Name = "author two" };

        var b1 = new BookEntity { Title = "book one" };
        var b2 = new BookEntity { Title = "book two" };

        await _db.SaveAsync([a1, a2]);
        await _db.SaveAsync([b1, b2]);

        await a1.Books.AddAsync([b1, b2]);
        await a2.Books.AddAsync([b1, b2]);

        await a1.Books.RemoveAsync([b1, b2]);

        var a2Books = await a2.Books.ChildrenQueryable().OrderBy(b => b.Title).ToListAsync();

        Assert.HasCount(2, a2Books);
        Assert.AreEqual(b1.Title, a2Books[0].Title);
        Assert.AreEqual(b2.Title, a2Books.Last().Title);
        Assert.AreEqual(0, await a1.Books.ChildrenCountAsync());
    }
}