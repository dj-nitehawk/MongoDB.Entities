using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

//NOTE: transactions are only supported on replica-sets. you need at least a single-node replica-set.
//      use mongod.cfg at root level of repo to run mongodb in replica-set mode
//      then run rs.initiate() in a mongo console

[TestClass]
public class TransactionsEntity
{
    [TestMethod]
    public async Task not_commiting_and_aborting_update_transaction_doesnt_modify_docs()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = "uwtrcd1", Surname = guid };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "uwtrcd2", Surname = guid };
        await db.SaveAsync(author2);
        var author3 = new AuthorEntity { Name = "uwtrcd3", Surname = guid };
        await db.SaveAsync(author3);

        using (var tn = db.Transaction())
        {
            await tn.Update<AuthorEntity>()
                    .Match(a => a.Surname == guid)
                    .Modify(a => a.Name, guid)
                    .Modify(a => a.Surname, author1.Name)
                    .ExecuteAsync();

            await tn.AbortAsync();

            //TN.CommitAsync();
        }

        var res = await db.Find<AuthorEntity>().OneAsync(author1.ID);

        Assert.AreEqual(author1.Name, res!.Name);
    }

    [TestMethod]
    public async Task commiting_update_transaction_modifies_docs()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = "uwtrcd1", Surname = guid };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "uwtrcd2", Surname = guid };
        await db.SaveAsync(author2);
        var author3 = new AuthorEntity { Name = "uwtrcd3", Surname = guid };
        await db.SaveAsync(author3);

        using (var tn = db.Transaction())
        {
            await tn.Update<AuthorEntity>()
                    .Match(a => a.Surname == guid)
                    .Modify(a => a.Name, guid)
                    .Modify(a => a.Surname, author1.Name)
                    .ExecuteAsync();

            await tn.CommitAsync();
        }

        var res = await db.Find<AuthorEntity>().OneAsync(author1.ID);

        Assert.AreEqual(guid, res!.Name);
    }

    [TestMethod]
    public async Task create_and_find_transaction_returns_correct_docs()
    {
        var book1 = new BookEntity { Title = "caftrcd1" };
        var book2 = new BookEntity { Title = "caftrcd1" };

        BookEntity? res;
        BookEntity fnt;

        using (var tn = DB.Default.Transaction())
        {
            await tn.SaveAsync(book1);
            await tn.SaveAsync(book2);

            _ = await tn.Find<BookEntity>().OneAsync(book1.ID);
            res = tn.Fluent<BookEntity>().Match(f => f.Eq(b => b.ID, book1.ID)).SingleOrDefault();
            _ = tn.Fluent<BookEntity>().FirstOrDefault();
            _ = tn.Fluent<BookEntity>().Match(b => b.ID == book2.ID).SingleOrDefault();
            fnt = tn.Fluent<BookEntity>().Match(f => f.Eq(b => b.ID, book2.ID)).SingleOrDefault();

            await tn.CommitAsync();
        }

        Assert.IsNotNull(res);
        Assert.AreEqual(book1.ID, res.ID);
        Assert.AreEqual(book2.ID, fnt.ID);
    }

    [TestMethod]
    public async Task delete_in_transaction_works()
    {
        var db = DB.Default;
        var book1 = new BookEntity { Title = "caftrcd1" };
        await db.SaveAsync(book1);

        using (var tn = db.Transaction())
        {
            await tn.DeleteAsync<BookEntity>(book1.ID);
            await tn.CommitAsync();
        }

        Assert.AreEqual(null, await DB.Default.Find<BookEntity>().OneAsync(book1.ID));
    }

    [TestMethod]
    public async Task full_text_search_transaction_returns_correct_results()
    {
        var db = DB.Default;

        await db.Index<AuthorEntity>()
                .Option(o => o.Background = false)
                .Key(a => a.Name, KeyType.Text)
                .Key(a => a.Surname, KeyType.Text)
                .CreateAsync();

        var author1 = new AuthorEntity { Name = "Name", Surname = Guid.NewGuid().ToString() };
        var author2 = new AuthorEntity { Name = "Name", Surname = Guid.NewGuid().ToString() };
        await db.SaveAsync(author1);
        await db.SaveAsync(author2);

        using var tn = db.Transaction();
        var tres = tn.FluentTextSearch<AuthorEntity>(Search.Full, author1.Surname).ToList();
        Assert.AreEqual(author1.Surname, tres[0].Surname);

        var tflu = tn.FluentTextSearch<AuthorEntity>(Search.Full, author2.Surname).SortByDescending(x => x.ModifiedOn).ToList();
        Assert.AreEqual(author2.Surname, tflu[0].Surname);
    }

    [TestMethod]
    public async Task bulk_save_entities_transaction_returns_correct_results()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();

        var entities = new[]
        {
            new BookEntity { Title = "one " + guid },
            new BookEntity { Title = "two " + guid },
            new BookEntity { Title = "thr " + guid }
        };

        using (var TN = db.Transaction())
        {
            await TN.SaveAsync(entities);
            await TN.CommitAsync();
        }

        var res = await db.Find<BookEntity>().ManyAsync(b => b.Title.Contains(guid));
        Assert.AreEqual(entities.Length, res.Count);

        foreach (var ent in res)
            ent.Title = "updated " + guid;
        await db.SaveAsync(res);

        res = await db.Find<BookEntity>().ManyAsync(b => b.Title.Contains(guid));
        Assert.AreEqual(3, res.Count);
        Assert.AreEqual("updated " + guid, res[0].Title);
    }
}