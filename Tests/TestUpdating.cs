using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Updating
    {
        [TestMethod]
        public void batch_updating_modifies_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; author1.Save();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; author3.Save();

            DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .Execute();

            var count = author1.Queryable().Where(a => a.Name == guid && a.Surname == author1.Name).Count();
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void batch_update_by_def_builder_mods_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; author1.Save();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; author3.Save();

            DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(b => b.Inc(a => a.Age, 10))
              .Modify(b => b.Set(a => a.Name, guid))
              .Modify(b => b.CurrentDate(a => a.ModifiedOn))
              .Execute();

            var res = DB.Find<Author>().Many(a => a.Surname == guid && a.Age == 10);

            Assert.AreEqual(2, res.Count());
            Assert.AreEqual(guid, res.First().Name);
        }

        [TestMethod]
        public void nested_properties_update_correctly()
        {
            var guid = Guid.NewGuid().ToString();

            var book = new Book
            {
                Title = "mnpuc title " + guid,
                Review = new Review { Rating = 10.10 }
            };
            book.Save();

            DB.Update<Book>()
                .Match(b => b.Review.Rating == 10.10)
                .Modify(b => b.Review.Rating, 22.22)
                .Execute();

            var res = DB.Find<Book>().One(book.ID);

            Assert.AreEqual(22.22, res.Review.Rating);
        }
    }
}
