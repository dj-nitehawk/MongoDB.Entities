using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
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
                  .Set(a => a.Name, guid)
                  .Set(a => a.Surname, author1.Name)
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
                  .Set(a => a.Name, guid)
                  .Set(a => a.Surname, author1.Name)
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

            using (var TN = new Transaction())
            {
                TN.Save(book1);
                TN.Save(book2);

                res = TN.Find<Book>().One(book1.ID);

                TN.Commit();
            }

            Assert.IsNotNull(res);
            Assert.AreEqual(book1.ID, res.ID);
        }

        //todo: delete

        //todo: searchtext

        //todo: create wiki page for transactions

        //todo: update wiki about DB.Find<T>().By to DB.Find<T>().Many
    }
}
