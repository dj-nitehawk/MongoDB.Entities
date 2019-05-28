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
              .Set(a => a.Name, guid)
              .Set(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .Execute();

            var count = author1.Collection().Where(a => a.Name == guid && a.Surname == author1.Name).Count();
            Assert.AreEqual(2, count);
        }
    }
}
