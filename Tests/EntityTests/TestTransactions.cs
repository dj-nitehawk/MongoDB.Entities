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
    public async Task audit_wrapper_saves_and_direct_saves_roll_back_on_abort()
    {
        await WithAuditDatabase(async db =>
        {
            var direct = new TransactionAuditEntity { Name = "direct" };
            var audited = new TransactionAuditEntity { Name = "audited" };

            using (var tx = db.WithModifiedBy(new ModifiedBy { UserID = "original" }).Transaction())
            {
                var wrapper = tx.WithModifiedBy(new ModifiedBy { UserID = "wrapper" });
                await tx.SaveAsync(direct);
                await wrapper.SaveAsync(audited);
                Assert.IsNotNull(await wrapper.Find<TransactionAuditEntity>().OneAsync(direct.ID));
                Assert.AreEqual("original", tx.ModifiedBy!.UserID);
                await tx.AbortAsync();
            }

            Assert.IsNull(await db.Find<TransactionAuditEntity>().OneAsync(direct.ID));
            Assert.IsNull(await db.Find<TransactionAuditEntity>().OneAsync(audited.ID));
        });
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task audit_wrapper_commit_and_disposal_preserve_transaction_boundaries(bool commit)
    {
        await WithAuditDatabase(async db =>
        {
            var direct = new TransactionAuditEntity { Name = "direct" };
            var audited = new TransactionAuditEntity { Name = "audited" };

            using (var tx = db.WithModifiedBy(new ModifiedBy { UserID = "original" }).Transaction())
            {
                var wrapper = tx.WithModifiedBy(new ModifiedBy { UserID = "wrapper" });
                await tx.SaveAsync(direct);
                await wrapper.SaveAsync(audited);
                if (commit)
                    await tx.CommitAsync();
            }

            var savedDirect = await db.Find<TransactionAuditEntity>().OneAsync(direct.ID);
            var savedAudited = await db.Find<TransactionAuditEntity>().OneAsync(audited.ID);
            if (commit)
            {
                Assert.IsNotNull(savedDirect);
                Assert.IsNotNull(savedAudited);
                Assert.AreEqual("original", savedDirect.ModifiedBy.UserID);
                Assert.AreEqual("wrapper", savedAudited.ModifiedBy.UserID);
            }
            else
            {
                Assert.IsNull(savedDirect);
                Assert.IsNull(savedAudited);
            }
        });
    }

    [TestMethod]
    public async Task audit_wrapper_updates_and_deletes_roll_back()
    {
        await WithAuditDatabase(async db =>
        {
            var original = db.WithModifiedBy(new ModifiedBy { UserID = "original" });
            var updated = new TransactionAuditEntity { Name = "before" };
            var deleted = new TransactionAuditEntity { Name = "keep" };
            await original.SaveAsync(updated);
            await original.SaveAsync(deleted);

            using (var tx = original.Transaction())
            {
                var wrapper = tx.WithModifiedBy(new ModifiedBy { UserID = "wrapper" });
                await wrapper.Update<TransactionAuditEntity>()
                             .MatchID(updated.ID)
                             .Modify(e => e.Name, "after")
                             .ExecuteAsync();
                await wrapper.DeleteAsync<TransactionAuditEntity>(deleted.ID);
                var inTransaction = await tx.Find<TransactionAuditEntity>().OneAsync(updated.ID);
                Assert.IsNotNull(inTransaction);
                Assert.AreEqual("after", inTransaction.Name);
                Assert.AreEqual("wrapper", inTransaction.ModifiedBy.UserID);
                Assert.IsNull(await tx.Find<TransactionAuditEntity>().OneAsync(deleted.ID));
                await tx.AbortAsync();
            }

            var restored = await db.Find<TransactionAuditEntity>().OneAsync(updated.ID);
            Assert.IsNotNull(restored);
            Assert.AreEqual("before", restored.Name);
            Assert.AreEqual("original", restored.ModifiedBy.UserID);
            Assert.IsNotNull(await db.Find<TransactionAuditEntity>().OneAsync(deleted.ID));
        });
    }

    [TestMethod]
    public async Task chained_audit_wrappers_preserve_session_database_and_global_filters()
    {
        await WithAuditDatabase(async db =>
        {
            var original = new FilteredAuditDb(db).WithModifiedBy(new ModifiedBy { UserID = "original" });
            var visible = new TransactionAuditEntity { Name = "visible" };
            var hidden = new TransactionAuditEntity { Name = "hidden" };

            using (var tx = original.Transaction())
            {
                var first = tx.WithModifiedBy(new ModifiedBy { UserID = "first" });
                var chained = first.WithModifiedBy(new ModifiedBy { UserID = "last" });
                Assert.AreSame(db.Database(), chained.Database());
                await tx.SaveAsync(hidden);
                await chained.SaveAsync(visible);
                Assert.IsNotNull(await chained.Find<TransactionAuditEntity>().OneAsync(visible.ID));
                Assert.IsNull(await chained.Find<TransactionAuditEntity>().OneAsync(hidden.ID));
                Assert.AreEqual("original", tx.ModifiedBy!.UserID);
                Assert.AreEqual("first", first.ModifiedBy!.UserID);
                first.IgnoreGlobalFilters = true;
                Assert.IsTrue(first.WithModifiedBy(new ModifiedBy()).IgnoreGlobalFilters);
                await tx.CommitAsync();
            }

            var saved = await db.Find<TransactionAuditEntity>().OneAsync(visible.ID);
            Assert.IsNotNull(saved);
            Assert.AreEqual("last", saved.ModifiedBy.UserID);
        });
    }

    [TestMethod]
    public async Task audit_wrapper_does_not_fall_back_to_sessionless_writes_after_disposal()
    {
        await WithAuditDatabase(async db =>
        {
            DB wrapper;
            using (var tx = db.Transaction())
                wrapper = tx.WithModifiedBy(new ModifiedBy { UserID = "wrapper" });

            var entity = new TransactionAuditEntity { Name = "must not persist" };
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => wrapper.SaveAsync(entity));
            Assert.IsNull(await db.Find<TransactionAuditEntity>().OneAsync(entity.ID));
        });
    }

    [TestMethod]
    public async Task ordinary_audit_wrappers_leave_source_audit_unchanged_and_persist_without_a_transaction()
    {
        await WithAuditDatabase(async db =>
        {
            var original = db.WithModifiedBy(new ModifiedBy { UserID = "original" });
            var wrapper = original.WithModifiedBy(new ModifiedBy { UserID = "wrapper" });
            var entity = new TransactionAuditEntity { Name = "persisted" };
            await wrapper.SaveAsync(entity);
            Assert.AreEqual("original", original.ModifiedBy!.UserID);
            Assert.IsNull(db.ModifiedBy);
            var saved = await db.Find<TransactionAuditEntity>().OneAsync(entity.ID);
            Assert.IsNotNull(saved);
            Assert.AreEqual("wrapper", saved.ModifiedBy.UserID);
        });
    }

    [TestMethod]
    public async Task transaction_created_from_audit_wrapper_owns_a_new_session()
    {
        await WithAuditDatabase(async db =>
        {
            using var original = db.WithModifiedBy(new ModifiedBy { UserID = "original" }).Transaction();
            var wrapper = original.WithModifiedBy(new ModifiedBy { UserID = "wrapper" });
            var entity = new TransactionAuditEntity { Name = "independent" };
            using (var independent = wrapper.Transaction())
            {
                Assert.AreNotSame(original.Session, independent.Session);
                await independent.SaveAsync(entity);
                await independent.CommitAsync();
            }
            await original.AbortAsync();
            var saved = await db.Find<TransactionAuditEntity>().OneAsync(entity.ID);
            Assert.IsNotNull(saved);
            Assert.AreEqual("wrapper", saved.ModifiedBy.UserID);
        });
    }

    static async Task WithAuditDatabase(Func<DB, Task> test)
    {
        var db = await InitTest.InitTestDatabase("transaction-audit-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Create the collection before any transaction starts.
            await db.Database().CreateCollectionAsync(nameof(TransactionAuditEntity));
            await test(db);
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.DatabaseName());
        }
    }

    public class TransactionAuditEntity : Entity
    {
        public string Name { get; set; } = "";
        public ModifiedBy ModifiedBy { get; set; } = null!;
    }

    class FilteredAuditDb : DB
    {
        public FilteredAuditDb(DB source) : base(source)
        {
            SetGlobalFilter<TransactionAuditEntity>(e => e.Name != "hidden");
        }
    }

    [TestMethod]
    public async Task not_commiting_and_aborting_update_transaction_doesnt_modify_docs()
    {
        var db = DB.Default.WithModifiedBy(new UpdatedBy());
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
        var db = DB.Default.WithModifiedBy(new UpdatedBy());
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

        using (var tn = DB.Default.WithModifiedBy(new UpdatedBy()).Transaction())
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
        var db = DB.Default.WithModifiedBy(new UpdatedBy());
        var book1 = new BookEntity { Title = "caftrcd1" };
        await db.SaveAsync(book1);

        using (var tn = db.Transaction())
        {
            await tn.DeleteAsync<BookEntity>(book1.ID);
            await tn.CommitAsync();
        }

        Assert.IsNull(await DB.Default.Find<BookEntity>().OneAsync(book1.ID));
    }

    [TestMethod]
    public async Task full_text_search_transaction_returns_correct_results()
    {
        var db = DB.Default.WithModifiedBy(new UpdatedBy());

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
        var db = DB.Default.WithModifiedBy(new UpdatedBy());
        var guid = Guid.NewGuid().ToString();

        var entities = new[]
        {
            new BookEntity { Title = "one " + guid },
            new BookEntity { Title = "two " + guid },
            new BookEntity { Title = "thr " + guid }
        };

        using (var tn = db.Transaction())
        {
            await tn.SaveAsync(entities);
            await tn.CommitAsync();
        }

        var res = await db.Find<BookEntity>().ManyAsync(b => b.Title.Contains(guid));
        Assert.HasCount(entities.Length, res);

        foreach (var ent in res)
            ent.Title = "updated " + guid;
        await db.SaveAsync(res);

        res = await db.Find<BookEntity>().ManyAsync(b => b.Title.Contains(guid));
        Assert.HasCount(3, res);
        Assert.AreEqual("updated " + guid, res[0].Title);
    }
}