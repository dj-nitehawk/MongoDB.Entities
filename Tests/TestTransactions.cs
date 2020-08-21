using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    //NOTE: transactions are only supported on replica-sets. you need at least a single-node replica-set.
    //      use mongod.cfg at root level of repo to run mongodb in replica-set mode
    //      then run rs.initiate() in a mongo console

    [TestClass]
    public class Transactions
    {
        [TestMethod]
        public void not_commiting_and_aborting_update_transaction_doesnt_modify_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "uwtrcd1", Surname = guid }; author1.Save();
            var author2 = new Author { Name = "uwtrcd2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "uwtrcd3", Surname = guid }; author3.Save();

            using (var TN = new Transaction())
            {
                TN.Update<Author>()
                  .Match(a => a.Surname == guid)
                  .Modify(a => a.Name, guid)
                  .Modify(a => a.Surname, author1.Name)
                  .Execute();

                TN.Abort();
                //TN.Commit();
            }

            var res = DB.Find<Author>().One(author1.ID);

            Assert.AreEqual(author1.Name, res.Name);
        }

        [TestMethod]
        public void commiting_update_transaction_modifies_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "uwtrcd1", Surname = guid }; author1.Save();
            var author2 = new Author { Name = "uwtrcd2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "uwtrcd3", Surname = guid }; author3.Save();

            using (var TN = new Transaction())
            {
                TN.Update<Author>()
                  .Match(a => a.Surname == guid)
                  .Modify(a => a.Name, guid)
                  .Modify(a => a.Surname, author1.Name)
                  .Execute();

                TN.Commit();
            }

            var res = DB.Find<Author>().One(author1.ID);

            Assert.AreEqual(guid, res.Name);
        }

        [TestMethod]
        public void create_and_find_transaction_returns_correct_docs()
        {
            var book1 = new Book { Title = "caftrcd1" };
            var book2 = new Book { Title = "caftrcd1" };

            Book res;
            Book fnt;

            using (var TN = new Transaction())
            {
                TN.Save(book1);
                TN.Save(book2);

                res = TN.Find<Book>().One(book1.ID);
                res = book1.Fluent(TN.Session).Match(f => f.Eq(b => b.ID, book1.ID)).SingleOrDefault();
                fnt = TN.Fluent<Book>().FirstOrDefault();
                fnt = TN.Fluent<Book>().Match(b => b.ID == book2.ID).SingleOrDefault();
                fnt = TN.Fluent<Book>().Match(f => f.Eq(b => b.ID, book2.ID)).SingleOrDefault();

                TN.Commit();
            }

            Assert.IsNotNull(res);
            Assert.AreEqual(book1.ID, res.ID);
            Assert.AreEqual(book2.ID, fnt.ID);
        }

        [TestMethod]
        public void delete_in_transaction_works()
        {
            var book1 = new Book { Title = "caftrcd1" };
            book1.Save();

            using (var TN = new Transaction())
            {
                TN.Delete<Book>(book1.ID);
                TN.Commit();
            }

            Assert.AreEqual(null, DB.Find<Book>().One(book1.ID));
        }

        [TestMethod]
        public void full_text_search_transaction_returns_correct_results()
        {
            DB.Index<Author>()
              .Option(o => o.Background = false)
              .Key(a => a.Name, KeyType.Text)
              .Key(a => a.Surname, KeyType.Text)
              .Create();

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            DB.Save(author1);
            DB.Save(author2);

            using (var TN = new Transaction())
            {
                var tres = TN.FluentTextSearch<Author>(Search.Full, author1.Surname).ToList(); ;
                Assert.AreEqual(author1.Surname, tres[0].Surname);

                var tflu = TN.FluentTextSearch<Author>(Search.Full, author2.Surname).SortByDescending(x => x.ModifiedOn).ToList(); ;
                Assert.AreEqual(author2.Surname, tflu[0].Surname);
            }
        }

        [TestMethod]
        public void bulk_save_entities_transaction_returns_correct_results()
        {
            var guid = Guid.NewGuid().ToString();

            var entities = new[] {
                new Book{Title="one "+guid},
                new Book{Title="two "+guid},
                new Book{Title="thr "+guid}
            };

            using (var TN = new Transaction())
            {
                TN.Save(entities);
                TN.Commit();
            }

            var res = DB.Find<Book>().Many(b => b.Title.Contains(guid));
            Assert.AreEqual(entities.Length, res.Count);

            foreach (var ent in res)
            {
                ent.Title = "updated " + guid;
            }
            res.Save();

            res = DB.Find<Book>().Many(b => b.Title.Contains(guid));
            Assert.AreEqual(3, res.Count);
            Assert.AreEqual("updated " + guid, res[0].Title);
        }
    }
}
