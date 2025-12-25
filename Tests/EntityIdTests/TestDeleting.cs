using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DeletingEntity
{
    readonly DB _db = DB.Default;

    public DeletingEntity()
    {
        _db = _db.WithModifiedBy(new());
    }

    [TestMethod]
    public async Task delete_by_id_removes_entity_from_collectionAsync()
    {
        var author1 = new AuthorEntity { Name = "auth1" };
        var author2 = new AuthorEntity { Name = "auth2" };
        var author3 = new AuthorEntity { Name = "auth3" };

        await _db.SaveAsync([author1, author2, author3]);

        await _db.DeleteAsync(author2);

        var a1 = await _db.Queryable<AuthorEntity>()
                          .Where(a => a.ID == author1.ID)
                          .SingleOrDefaultAsync();

        var a2 = await _db.Queryable<AuthorEntity>()
                          .Where(a => a.ID == author2.ID)
                          .SingleOrDefaultAsync();

        Assert.IsNull(a2);
        Assert.AreEqual(author1.Name, a1.Name);
    }

    [TestMethod]
    public async Task deleting_entity_removes_all_refs_to_itselfAsync()
    {
        var author = new AuthorEntity { Name = "author" };
        var book1 = new BookEntity { Title = "derarti1" };
        var book2 = new BookEntity { Title = "derarti2" };

        await _db.SaveAsync(book1);
        await _db.SaveAsync(book2);
        await _db.SaveAsync(author);

        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);

        await book1.GoodAuthors.AddAsync(author);
        await book2.GoodAuthors.AddAsync(author);

        await _db.DeleteAsync(author);
        Assert.AreEqual(0, await book2.GoodAuthors.ChildrenQueryable().CountAsync());

        await _db.DeleteAsync(book1);
        Assert.AreEqual(0, await author.Books.ChildrenQueryable().CountAsync());
    }

    [TestMethod]
    public async Task deleteall_removes_entity_and_refs_to_itselfAsync()
    {
        var book = new BookEntity { Title = "Test" };
        await _db.SaveAsync(book);
        var author1 = new AuthorEntity { Name = "ewtrcd1" };
        await _db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "ewtrcd2" };
        await _db.SaveAsync(author2);
        await book.GoodAuthors.AddAsync(author1);
        book.OtherAuthors = [author1, author2];
        await _db.SaveAsync(book);
        await book.OtherAuthors.DeleteAllAsync(_db);
        Assert.AreEqual(0, await book.GoodAuthors.ChildrenQueryable().CountAsync());
        Assert.IsNull(await _db.Queryable<AuthorEntity>().Where(a => a.ID == author1.ID).SingleOrDefaultAsync());
    }

    [TestMethod]
    public async Task deleting_a_one2many_ref_entity_makes_parent_nullAsync()
    {
        var book = new BookEntity { Title = "Test" };
        await _db.SaveAsync(book);
        var author = new AuthorEntity { Name = "ewtrcd1" };
        await _db.SaveAsync(author);
        book.MainAuthor = author.ToReference();
        await _db.SaveAsync(book);
        await _db.DeleteAsync(author);
        Assert.IsNull(await book.MainAuthor.ToEntityAsync(_db));
    }

    [TestMethod]
    public async Task delete_by_expression_deletes_all_matchesAsync()
    {
        var author1 = new AuthorEntity { Name = "xxx" };
        await _db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "xxx" };
        await _db.SaveAsync(author2);

        await _db.DeleteAsync<AuthorEntity>(x => x.Name == "xxx");

        var count = await _db.Queryable<AuthorEntity>()
                             .CountAsync(a => a.Name == "xxx");

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task high_volume_deletes_with_idsAsync()
    {
        var ds = new List<string>(100100);

        for (var i = 0; i < 100100; i++)
            ds.Add(ObjectId.GenerateNewId().ToString()!);

        await _db.DeleteAsync<Blank>(ds);
    }

    [TestCategory("SkipWhenLiveUnitTesting"), TestMethod]
    public async Task high_volume_deletes_with_expressionAsync()
    {
        //start with clean collection
        await _db.DropCollectionAsync<Blank>();

        var list = new List<Blank>(100100);
        for (var i = 0; i < 100100; i++)
            list.Add(new());
        await _db.SaveAsync(list);

        Assert.AreEqual(100100, _db.Queryable<Blank>().Count());

        await _db.DeleteAsync<Blank>(_ => true);

        Assert.AreEqual(0, await _db.CountAsync<Blank>());

        //reclaim disk space
        await _db.DropCollectionAsync<Blank>();
        await _db.SaveAsync(new Blank());
    }

    [TestMethod]
    public async Task delete_by_ids_with_global_filter()
    {
        var db = new MyDbEntity();

        var a1 = new AuthorEntity { Age = 10 };
        var a2 = new AuthorEntity { Age = 111 };
        var a3 = new AuthorEntity { Age = 111 };

        await db.SaveAsync([a1, a2, a3]);

        var ds = new[] { a1.ID, a2.ID, a3.ID };

        var res = await db.DeleteAsync<AuthorEntity>(ds);

        var notDeletedIDs = await _db.Find<AuthorEntity, string>()
                                     .Match(a => ds.Contains(a.ID))
                                     .Project(a => a.ID)
                                     .ExecuteAsync();

        Assert.AreEqual(2, res.DeletedCount);
        Assert.AreEqual(a1.ID, notDeletedIDs.Single());
    }
}