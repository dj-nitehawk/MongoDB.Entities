using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class RelationshipsObjectId
{
    [TestMethod]
    public async Task setting_one_to_one_reference_returns_correct_ObjectId()
    {
        var book = new BookObjectId { Title = "book" };
        var author = new AuthorObjectId { Name = "sotorrce" };
        await author.SaveAsync();
        book.MainAuthor = new(author);
        await book.SaveAsync();
        var res = await (await book.Queryable()
                                   .Where(b => b.ID == book.ID)
                                   .SingleAsync())
                        .MainAuthor.ToEntityAsync();
        Assert.AreEqual(author.Name, res.Name);
    }

    [TestMethod]
    public async Task setting_one_to_one_reference_with_implicit_operator_by_string_id_returns_correct_ObjectId()
    {
        var book = new BookObjectId { Title = "book" };
        var author = new AuthorObjectId { Name = "sotorrce" };
        await author.SaveAsync();
        book.MainAuthor = new(author);
        await book.SaveAsync();
        var res = await (await book.Queryable()
                                   .Where(b => b.ID == book.ID)
                                   .SingleAsync())
                        .MainAuthor.ToEntityAsync();
        Assert.AreEqual(author.Name, res.Name);
    }

    [TestMethod]
    public async Task setting_one_to_one_reference_with_implicit_operator_by_ObjectId_returns_correct_ObjectId()
    {
        var book = new BookObjectId { Title = "book" };
        var author = new AuthorObjectId { Name = "soorfioberce" };
        await author.SaveAsync();
        book.MainAuthor = new(author);
        await book.SaveAsync();
        var res = await (await book.Queryable()
                                   .Where(b => b.ID == book.ID)
                                   .SingleAsync())
                        .MainAuthor.ToEntityAsync();
        Assert.AreEqual(author.Name, res.Name);
    }

    [TestMethod]
    public async Task one_to_one_to_ObjectId_with_lambda_projection()
    {
        var book = new BookObjectId { Title = "book" };
        var author = new AuthorObjectId { Name = "ototoewlp" };
        await author.SaveAsync();
        book.MainAuthor = new(author);
        await book.SaveAsync();
        var res = await (await book.Queryable()
                                   .Where(b => b.ID == book.ID)
                                   .SingleAsync())
                        .MainAuthor.ToEntityAsync(a => new() { Name = a.Name });
        Assert.AreEqual(author.Name, res.Name);
        Assert.IsTrue(res.HasDefaultID());
    }

    [TestMethod]
    public async Task one_to_one_to_ObjectId_with_mongo_projection()
    {
        var book = new BookObjectId { Title = "book" };
        var author = new AuthorObjectId { Name = "ototoewmp" };
        await author.SaveAsync();
        book.MainAuthor = new(author);
        await book.SaveAsync();
        var res = await (await book.Queryable()
                                   .Where(b => b.ID == book.ID)
                                   .SingleAsync())
                        .MainAuthor.ToEntityAsync(p => p.Include(a => a.Name).Exclude(a => a.ID));
        Assert.AreEqual(author.Name, res.Name);
        Assert.IsTrue(res.HasDefaultID());
    }

    [TestMethod]
    public async Task adding_one2many_references_returns_correct_entities_queryable()
    {
        var author = new AuthorObjectId { Name = "author" };
        var book1 = new BookObjectId { Title = "aotmrrceb1" };
        var book2 = new BookObjectId { Title = "aotmrrceb2" };
        await book1.SaveAsync();
        await book2.SaveAsync();
        await author.SaveAsync();
        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);
        var books = await author.Queryable()
                                .Where(a => a.ID == author.ID)
                                .Single()
                                .Books
                                .ChildrenQueryable().ToListAsync();
        Assert.AreEqual(book2.Title, books[1].Title);
    }

    [TestMethod]
    public async Task ienumerable_for_many()
    {
        var author = new AuthorObjectId { Name = "author" };
        var book1 = new BookObjectId { Title = "aotmrrceb1" };
        var book2 = new BookObjectId { Title = "aotmrrceb2" };
        await book1.SaveAsync();
        await book2.SaveAsync();
        await author.SaveAsync();
        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);
        var books = (await author.Queryable()
                                 .Where(a => a.ID == author.ID)
                                 .SingleAsync())
            .Books;

        List<BookObjectId> booklist = new();

        booklist.AddRange(books);

        Assert.AreEqual(2, booklist.Count);
    }

    [TestMethod]
    public async Task adding_one2many_references_returns_correct_entities_fluent()
    {
        var author = new AuthorObjectId { Name = "author" };
        var book1 = new BookObjectId { Title = "aotmrrcebf1" };
        var book2 = new BookObjectId { Title = "aotmrrcebf2" };
        await book1.SaveAsync();
        await book2.SaveAsync();
        await author.SaveAsync();
        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);
        var books = await author.Queryable()
                                .Where(a => a.ID == author.ID)
                                .Single()
                                .Books
                                .ChildrenFluent().ToListAsync();
        Assert.AreEqual(book2.Title, books[1].Title);
    }

    [TestMethod]
    public async Task removing_a_one2many_ref_removes_correct_entities()
    {
        var book = new BookObjectId { Title = "rotmrrceb" };
        await book.SaveAsync();
        var author1 = new AuthorObjectId { Name = "rotmrrcea1" };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = "rotmrrcea2" };
        await author2.SaveAsync();
        await book.GoodAuthors.AddAsync(author1);
        await book.GoodAuthors.AddAsync(author2);
        var remAuthor = await DB.Instance().Queryable<AuthorObjectId>()
                                .Where(a => a.ID == author2.ID)
                                .SingleAsync();
        await book.GoodAuthors.RemoveAsync(remAuthor);
        var resBook = await book.Queryable()
                                .Where(b => b.ID == book.ID)
                                .SingleAsync();
        Assert.AreEqual(1, await resBook.GoodAuthors.ChildrenQueryable().CountAsync());
        Assert.AreEqual(author1.Name, (await resBook.GoodAuthors.ChildrenQueryable().FirstAsync()).Name);
    }

    [TestMethod]
    public async Task collection_shortcut_of_many_returns_correct_children()
    {
        var book = new BookObjectId { Title = "book" };
        await book.SaveAsync();
        var author1 = new AuthorObjectId { Name = "csomrcc1" };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = "csomrcc1" };
        await author2.SaveAsync();
        await book.GoodAuthors.AddAsync(author1);
        await book.GoodAuthors.AddAsync(author2);
        Assert.AreEqual(2, await book.GoodAuthors.ChildrenQueryable().CountAsync());
        Assert.AreEqual(author1.Name, (await book.GoodAuthors.ChildrenQueryable().FirstAsync()).Name);
    }

    [TestMethod]
    public void accessing_coll_shortcut_on_unsaved_parent_throws()
    {
        var book = new BookObjectId { Title = "acsoupt" };
        Assert.ThrowsException<InvalidOperationException>(() => book.GoodAuthors.ChildrenQueryable().Count());
    }

    [TestMethod]
    public async Task many_children_count()
    {
        var book1 = new BookObjectId { Title = "mcc" };
        await book1.SaveAsync();
        var gen1 = new GenreObjectId { Name = "ac2mrceg1" };
        await gen1.SaveAsync();
        var gen2 = new GenreObjectId { Name = "ac2mrceg1" };
        await gen2.SaveAsync();

        await book1.Genres.AddAsync(gen1);
        await book1.Genres.AddAsync(gen2);

        Assert.AreEqual(2, await book1.Genres.ChildrenCountAsync());

        var book2 = new BookObjectId { Title = "mcc" };
        await book2.SaveAsync();

        await gen1.Books.AddAsync(book1);
        await gen1.Books.AddAsync(book2);

        Assert.AreEqual(2, await gen1.Books.ChildrenCountAsync());
    }

    [TestMethod]
    public async Task adding_many2many_returns_correct_children()
    {
        var book1 = new BookObjectId { Title = "ac2mrceb1" };
        await book1.SaveAsync();
        var book2 = new BookObjectId { Title = "ac2mrceb2" };
        await book2.SaveAsync();

        var gen1 = new GenreObjectId { Name = "ac2mrceg1" };
        await gen1.SaveAsync();
        var gen2 = new GenreObjectId { Name = "ac2mrceg1" };
        await gen2.SaveAsync();

        await book1.Genres.AddAsync(gen1);
        await book1.Genres.AddAsync(gen2);
        await book1.Genres.AddAsync(gen1);
        Assert.AreEqual(2, DB.Instance().Queryable<BookObjectId>().Where(b => b.ID == book1.ID).Single().Genres.ChildrenQueryable().Count());
        Assert.AreEqual(gen1.Name, book1.Genres.ChildrenQueryable().First().Name);

        await gen1.Books.AddAsync(book1);
        await gen1.Books.AddAsync(book2);
        await gen1.Books.AddAsync(book1);
        Assert.AreEqual(2, gen1.Queryable().Where(g => g.ID == gen1.ID).Single().Books.ChildrenQueryable().Count());
        Assert.AreEqual(gen1.Name, book2.Queryable().Where(b => b.ID == book2.ID).Single().Genres.ChildrenQueryable().First().Name);

        await gen2.Books.AddAsync(book1);
        await gen2.Books.AddAsync(book2);
        Assert.AreEqual(2, book1.Genres.ChildrenQueryable().Count());
        Assert.AreEqual(gen2.Name, book2.Queryable().Where(b => b.ID == book2.ID).Single().Genres.ChildrenQueryable().First().Name);
    }

    [TestMethod]
    public async Task removing_many2many_returns_correct_children()
    {
        var book1 = new BookObjectId { Title = "rm2mrceb1" };
        await book1.SaveAsync();
        var book2 = new BookObjectId { Title = "rm2mrceb2" };
        await book2.SaveAsync();

        var gen1 = new GenreObjectId { Name = "rm2mrceg1" };
        await gen1.SaveAsync();
        var gen2 = new GenreObjectId { Name = "rm2mrceg1" };
        await gen2.SaveAsync();

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

    //[TestMethod]
    //public async Task getting_parents_of_a_relationship_queryable_works()
    //{
    //    var ObjectId = ObjectId.NewObjectId().ToString();

    //    var book = new BookObjectId { Title = "Planet Of The Apes " + ObjectId };
    //    await book.SaveAsync();

    //    var genre = new GenreObjectId { Name = "SciFi " + ObjectId };
    //    await genre.SaveAsync();

    //    var genre1 = new GenreObjectId { Name = "Thriller " + ObjectId };
    //    await genre1.SaveAsync();

    //    await book.Genres.AddAsync(genre);
    //    await book.Genres.AddAsync(genre1);

    //    var books = await book.Genres
    //                    .ParentsQueryable<BookObjectId>(genre.ID)
    //                    .ToListAsync();

    //    Assert.AreEqual(1, books.Count);
    //    Assert.AreEqual(book.Title, books.Single().Title);

    //    //var y = (await genre.Queryable().Where(g => g.Name.Contains(ObjectId)).ToListAsync()).Select(x => x.ID);
    //    //var x = book.Genres.ParentsQueryable<BookObjectId>(y);
    //    //books = await x.ToListAsync();

    //    books = await book.Genres
    //            .ParentsQueryable<BookObjectId>(genre.Queryable().Where(g => g.Name.Contains(ObjectId)))
    //            .ToListAsync();

    //    Assert.AreEqual(1, books.Count);
    //    Assert.AreEqual(book.Title, books.Single(b => b.ID == book.ID).Title);

    //    var genres = await genre.Books
    //                      .ParentsQueryable<Genre>(new[] { book.ID, book.ID })
    //                      .ToListAsync();

    //    Assert.AreEqual(2, genres.Count);
    //    Assert.AreEqual(genre.Name, genres.First(g => g.ID == genre.ID).Name);

    //    genres = await genre.Books
    //             .ParentsQueryable<Genre>(book.Queryable().Where(b => b.ID == book.ID))
    //             .ToListAsync();

    //    Assert.AreEqual(2, genres.Count);
    //    Assert.IsTrue(genres.Any(g => g.ID == genre.ID));
    //}

    [TestMethod]
    public async Task getting_parents_of_a_relationship_fluent_works()
    {
        var guid = Guid.NewGuid().ToString();

        var book = new BookObjectId { Title = "Planet Of The Apes " + guid };
        await book.SaveAsync();

        var genre = new GenreObjectId { Name = "SciFi " + guid };
        await genre.SaveAsync();

        var genre1 = new GenreObjectId { Name = "Thriller " + guid };
        await genre1.SaveAsync();

        await book.Genres.AddAsync(genre);
        await book.Genres.AddAsync(genre1);

        var books = await book.Genres
                              .ParentsFluent(genre.ID)
                              .ToListAsync();

        Assert.AreEqual(1, books.Count);
        Assert.AreEqual(book.Title, books.Single().Title);

        books = await book.Genres
                          .ParentsFluent(genre.Fluent().Match(g => g.Name.Contains(guid)))
                          .ToListAsync();

        Assert.AreEqual(1, books.Count);
        Assert.AreEqual(book.Title, books.Single().Title);

        var genres = await genre.Books
                                .ParentsFluent(book.ID)
                                .ToListAsync();

        Assert.AreEqual(2, genres.Count);
        Assert.AreEqual(genre.Name, genres.Single(g => g.ID == genre.ID).Name);

        genres = await genre.Books
                            .ParentsFluent(book.Fluent().Match(b => b.ID == book.ID))
                            .ToListAsync();

        Assert.AreEqual(1, books.Count);
        Assert.AreEqual(book.Title, books.Single().Title);
    }

    [TestMethod]
    public async Task add_child_to_many_relationship_with_ID()
    {
        var author = new AuthorObjectId { Name = "author" };
        await author.SaveAsync();

        var b1 = new BookObjectId { Title = "book1" };
        await b1.SaveAsync();
        var b2 = new BookObjectId { Title = "book2" };
        await b2.SaveAsync();

        await author.Books.AddAsync(b1.ID);
        await author.Books.AddAsync(b2.ID);

        var books = await author.Books
                                .ChildrenQueryable()
                                .OrderBy(b => b.Title)
                                .ToListAsync();

        Assert.AreEqual(2, books.Count);
        Assert.IsTrue(books[0].Title == "book1");
        Assert.IsTrue(books[1].Title == "book2");
    }

    [TestMethod]
    public async Task relationships_with_custom_ID()
    {
        var customer = new CustomerWithCustomID();
        await customer.SaveAsync();

        var flower = new FlowerObjectId { Name = customer.ID };
        await flower.SaveAsync();

        var flower2 = new FlowerObjectId();
        await flower2.SaveAsync();

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
        var author = new AuthorObjectId { Name = "author" };
        await author.SaveAsync();

        var b1 = new BookObjectId { Title = "book1" };
        await b1.SaveAsync();
        var b2 = new BookObjectId { Title = "book2" };
        await b2.SaveAsync();

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
        var author = new AuthorObjectId { Name = "author" };
        await author.SaveAsync();

        var b1 = new BookObjectId { Title = "book1" };
        await b1.SaveAsync();
        var b2 = new BookObjectId { Title = "book2" };
        await b2.SaveAsync();

        await author.Books.AddAsync(b1);
        await author.Books.AddAsync(b2.ID);

        var books = await author.Books
                                .ChildrenQueryable()
                                .OrderBy(b => b.Title)
                                .ToListAsync();

        Assert.AreEqual(2, books.Count);
        Assert.IsTrue(books[0].Title == "book1");
        Assert.IsTrue(books[1].Title == "book2");
    }

    [TestMethod]
    public async Task overload_operator_for_removing_children_from_many_relationships()
    {
        var author = new AuthorObjectId { Name = "author" };
        await author.SaveAsync();

        var b1 = new BookObjectId { Title = "book1" };
        await b1.SaveAsync();
        var b2 = new BookObjectId { Title = "book2" };
        await b2.SaveAsync();

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
        var a1 = new AuthorObjectId { Name = "author one" };
        var a2 = new AuthorObjectId { Name = "author two" };

        var b1 = new BookObjectId { Title = "book one" };
        var b2 = new BookObjectId { Title = "book two" };

        await new[] { a1, a2 }.SaveAsync();
        await new[] { b1, b2 }.SaveAsync();

        await a1.Books.AddAsync(new[] { b1, b2 });
        await a2.Books.AddAsync(new[] { b1, b2 });

        await a1.Books.RemoveAsync(new[] { b1, b2 });

        var a2books = await a2.Books.ChildrenQueryable().OrderBy(b => b.Title).ToListAsync();

        Assert.AreEqual(2, a2books.Count);
        Assert.AreEqual(b1.Title, a2books[0].Title);
        Assert.AreEqual(b2.Title, a2books.Last().Title);
        Assert.AreEqual(0, await a1.Books.ChildrenCountAsync());
    }
}