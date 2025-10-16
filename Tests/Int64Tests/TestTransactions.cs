using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

//NOTE: transactions are only supported on replica-sets. you need at least a single-node replica-set.
//      use mongod.cfg at root level of repo to run mongodb in replica-set mode
//      then run rs.initiate() in a mongo console

[TestClass]
public class TransactionsInt64
{
    [TestMethod]
    public async Task not_commiting_and_aborting_update_transaction_doesnt_modify_docs()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorInt64 { Name = "uwtrcd1", Surname = guid }; await author1.SaveAsync();
        var author2 = new AuthorInt64 { Name = "uwtrcd2", Surname = guid }; await author2.SaveAsync();
        var author3 = new AuthorInt64 { Name = "uwtrcd3", Surname = guid }; await author3.SaveAsync();

        using (var TN = new Transaction(modifiedBy: new()))
        {
            await TN.Update<AuthorInt64>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .ExecuteAsync();

            await TN.AbortAsync();
            //TN.CommitAsync();
        }

        var res = await DB.Instance().Find<AuthorInt64>().OneAsync(author1.ID);

        Assert.AreEqual(author1.Name, res!.Name);
    }

    [TestMethod]
    public async Task commiting_update_transaction_modifies_docs()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorInt64 { Name = "uwtrcd1", Surname = guid }; await author1.SaveAsync();
        var author2 = new AuthorInt64 { Name = "uwtrcd2", Surname = guid }; await author2.SaveAsync();
        var author3 = new AuthorInt64 { Name = "uwtrcd3", Surname = guid }; await author3.SaveAsync();

        using (var TN = new Transaction(modifiedBy: new()))
        {
            await TN.Update<AuthorInt64>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .ExecuteAsync();

            await TN.CommitAsync();
        }

        var res = await DB.Instance().Find<AuthorInt64>().OneAsync(author1.ID);

        Assert.AreEqual(guid, res!.Name);
    }

    [TestMethod]
    public async Task commiting_update_transaction_modifies_docs_dbcontext()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorInt64 { Name = "uwtrcd1", Surname = guid }; await author1.SaveAsync();
        var author2 = new AuthorInt64 { Name = "uwtrcd2", Surname = guid }; await author2.SaveAsync();
        var author3 = new AuthorInt64 { Name = "uwtrcd3", Surname = guid }; await author3.SaveAsync();

        var db = new DBContext(modifiedBy: new());

        using (db.Transaction())
        {
            await db.Update<AuthorInt64>()
                    .Match(a => a.Surname == guid)
                    .Modify(a => a.Name, guid)
                    .Modify(a => a.Surname, author1.Name)
                    .ExecuteAsync();

            await db.CommitAsync();
        }

        var res = await DB.Instance().Find<AuthorInt64>().OneAsync(author1.ID);

        Assert.AreEqual(guid, res!.Name);
    }

    [TestMethod]
    public async Task create_and_find_transaction_returns_correct_docs()
    {
        var book1 = new BookInt64 { Title = "caftrcd1" };
        var book2 = new BookInt64 { Title = "caftrcd1" };

        BookInt64? res;
        BookInt64 fnt;

        using (var TN = new Transaction(modifiedBy: new()))
        {
            await TN.SaveAsync(book1);
            await TN.SaveAsync(book2);

            res = await TN.Find<BookInt64>().OneAsync(book1.ID);
            res = book1.Fluent(null, TN.Session).Match(f => f.Eq(b => b.ID, book1.ID)).SingleOrDefault();
            fnt = TN.Fluent<BookInt64>().FirstOrDefault();
            fnt = TN.Fluent<BookInt64>().Match(b => b.ID == book2.ID).SingleOrDefault();
            fnt = TN.Fluent<BookInt64>().Match(f => f.Eq(b => b.ID, book2.ID)).SingleOrDefault();

            await TN.CommitAsync();
        }

        Assert.IsNotNull(res);
        Assert.AreEqual(book1.ID, res.ID);
        Assert.AreEqual(book2.ID, fnt.ID);
    }

    [TestMethod]
    public async Task delete_in_transaction_works()
    {
        var book1 = new BookInt64 { Title = "caftrcd1" };
        await book1.SaveAsync();

        using (var TN = new Transaction())
        {
            await TN.DeleteAsync<BookInt64>(book1.ID);
            await TN.CommitAsync();
        }

        Assert.AreEqual(null, await DB.Instance().Find<BookInt64>().OneAsync(book1.ID));
    }

    [TestMethod]
    public async Task full_text_search_transaction_returns_correct_results()
    {
        await DB.Instance().Index<AuthorInt64>()
          .Option(o => o.Background = false)
          .Key(a => a.Name, KeyType.Text)
          .Key(a => a.Surname, KeyType.Text)
          .CreateAsync();

        var author1 = new AuthorInt64 { Name = "Name", Surname = Guid.NewGuid().ToString() };
        var author2 = new AuthorInt64 { Name = "Name", Surname = Guid.NewGuid().ToString() };
        await DB.Instance().SaveAsync(author1);
        await DB.Instance().SaveAsync(author2);

        using var TN = new Transaction();
        var tres = TN.FluentTextSearch<AuthorInt64>(Search.Full, author1.Surname).ToList();
        Assert.AreEqual(author1.Surname, tres[0].Surname);

        var tflu = TN.FluentTextSearch<AuthorInt64>(Search.Full, author2.Surname).SortByDescending(x => x.ModifiedOn).ToList();
        Assert.AreEqual(author2.Surname, tflu[0].Surname);
    }

    [TestMethod]
    public async Task bulk_save_entities_transaction_returns_correct_results()
    {
        var guid = Guid.NewGuid().ToString();

        var entities = new[] {
            new BookInt64{Title="one "+guid},
            new BookInt64{Title="two "+guid},
            new BookInt64{Title="thr "+guid}
        };

        using (var TN = new Transaction(modifiedBy: new()))
        {
            await TN.SaveAsync(entities);
            await TN.CommitAsync();
        }

        var res = await DB.Instance().Find<BookInt64>().ManyAsync(b => b.Title.Contains(guid));
        Assert.AreEqual(entities.Length, res.Count);

        foreach (var ent in res)
        {
            ent.Title = "updated " + guid;
        }
        await res.SaveAsync();

        res = await DB.Instance().Find<BookInt64>().ManyAsync(b => b.Title.Contains(guid));
        Assert.AreEqual(3, res.Count);
        Assert.AreEqual("updated " + guid, res[0].Title);
    }
}
