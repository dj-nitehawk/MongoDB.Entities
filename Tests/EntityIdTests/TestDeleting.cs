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
    [TestMethod]
    public async Task delete_by_id_removes_entity_from_collectionAsync()
    {
        var author1 = new AuthorEntity { Name = "auth1" };
        var author2 = new AuthorEntity { Name = "auth2" };
        var author3 = new AuthorEntity { Name = "auth3" };

        await new[] { author1, author2, author3 }.SaveAsync();

        await author2.DeleteAsync();

        var a1 = await author1.Queryable()
                              .Where(a => a.ID == author1.ID)
                              .SingleOrDefaultAsync();

        var a2 = await author2.Queryable()
                              .Where(a => a.ID == author2.ID)
                              .SingleOrDefaultAsync();

        Assert.AreEqual(null, a2);
        Assert.AreEqual(author1.Name, a1.Name);
    }

    [TestMethod]
    public async Task deleting_entity_removes_all_refs_to_itselfAsync()
    {
        var author = new AuthorEntity { Name = "author" };
        var book1 = new BookEntity { Title = "derarti1" };
        var book2 = new BookEntity { Title = "derarti2" };

        await book1.SaveAsync();
        await book2.SaveAsync();
        await author.SaveAsync();

        await author.Books.AddAsync(book1);
        await author.Books.AddAsync(book2);

        await book1.GoodAuthors.AddAsync(author);
        await book2.GoodAuthors.AddAsync(author);

        await author.DeleteAsync();
        Assert.AreEqual(0, await book2.GoodAuthors.ChildrenQueryable().CountAsync());

        await book1.DeleteAsync();
        Assert.AreEqual(0, await author.Books.ChildrenQueryable().CountAsync());
    }

    [TestMethod]
    public async Task deleteall_removes_entity_and_refs_to_itselfAsync()
    {
        var book = new BookEntity { Title = "Test" };
        await book.SaveAsync();
        var author1 = new AuthorEntity { Name = "ewtrcd1" };
        await author1.SaveAsync();
        var author2 = new AuthorEntity { Name = "ewtrcd2" };
        await author2.SaveAsync();
        await book.GoodAuthors.AddAsync(author1);
        book.OtherAuthors = new[] { author1, author2 };
        await book.SaveAsync();
        await book.OtherAuthors.DeleteAllAsync();
        Assert.AreEqual(0, await book.GoodAuthors.ChildrenQueryable().CountAsync());
        Assert.AreEqual(null, await author1.Queryable().Where(a => a.ID == author1.ID).SingleOrDefaultAsync());
    }

    [TestMethod]
    public async Task deleting_a_one2many_ref_entity_makes_parent_nullAsync()
    {
        var book = new BookEntity { Title = "Test" };
        await book.SaveAsync();
        var author = new AuthorEntity { Name = "ewtrcd1" };
        await author.SaveAsync();
        book.MainAuthor = author.ToReference();
        await book.SaveAsync();
        await author.DeleteAsync();
        Assert.AreEqual(null, await book.MainAuthor.ToEntityAsync());
    }

    [TestMethod]
    public async Task delete_by_expression_deletes_all_matchesAsync()
    {
        var author1 = new AuthorEntity { Name = "xxx" };
        await author1.SaveAsync();
        var author2 = new AuthorEntity { Name = "xxx" };
        await author2.SaveAsync();

        var db = DB.Default;

        await db.DeleteAsync<AuthorEntity>(x => x.Name == "xxx");

        var count = await db.Queryable<AuthorEntity>()
                            .CountAsync(a => a.Name == "xxx");

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task high_volume_deletes_with_idsAsync()
    {
        var IDs = new List<string>(100100);

        for (var i = 0; i < 100100; i++)
            IDs.Add(ObjectId.GenerateNewId().ToString()!);

        await DB.Default.DeleteAsync<Blank>(IDs);
    }

    [TestCategory("SkipWhenLiveUnitTesting"), TestMethod]
    public async Task high_volume_deletes_with_expressionAsync()
    {
        var db = DB.Default;

        //start with clean collection
        await db.DropCollectionAsync<Blank>();

        var list = new List<Blank>(100100);
        for (var i = 0; i < 100100; i++)
            list.Add(new());
        await list.SaveAsync();

        Assert.AreEqual(100100, db.Queryable<Blank>().Count());

        await db.DeleteAsync<Blank>(_ => true);

        Assert.AreEqual(0, await db.CountAsync<Blank>());

        //reclaim disk space
        await db.DropCollectionAsync<Blank>();
        await db.SaveAsync(new Blank());
    }

    [TestMethod]
    public async Task delete_by_ids_with_global_filter()
    {
        var dbEntity = new MyDBEntity();

        var a1 = new AuthorEntity { Age = 10 };
        var a2 = new AuthorEntity { Age = 111 };
        var a3 = new AuthorEntity { Age = 111 };

        await new[] { a1, a2, a3 }.SaveAsync();

        var IDs = new[] { a1.ID, a2.ID, a3.ID };

        var res = await dbEntity.DeleteAsync<AuthorEntity>(IDs);

        var db = DB.Default;

        var notDeletedIDs = await db.Find<AuthorEntity, string>()
                                    .Match(a => IDs.Contains(a.ID))
                                    .Project(a => a.ID)
                                    .ExecuteAsync();

        Assert.AreEqual(2, res.DeletedCount);
        Assert.IsTrue(notDeletedIDs.Single() == a1.ID);
    }
}