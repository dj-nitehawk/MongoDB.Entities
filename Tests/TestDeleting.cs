using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests;
using System.Linq;

namespace MongoDB.Entities.Test
{
    [TestClass]
    public class MyTestClass
    {
        [TestMethod]
        public void delete_by_id_removes_entity_from_collection()
        {
            var author1 = new Author { Name = "auth1" }; author1.Save();
            var author2 = new Author { Name = "auth2" }; author2.Save();
            var author3 = new Author { Name = "auth3" }; author3.Save();

            author2.Delete();

            var a1 = author1.Collection()
                             .Where(a => a.ID == author1.ID)
                             .SingleOrDefault();
            var a2 = author2.Collection()
                              .Where(a => a.ID == author2.ID)
                              .SingleOrDefault();

            Assert.AreEqual(null, a2);
            Assert.AreEqual(author1.Name, a1.Name);
        }

        [TestMethod]
        public void deleting_entity_removes_all_refs_to_itself()
        {
            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "derarti1" };
            var book2 = new Book { Title = "derarti2" };

            book1.Save();
            book2.Save();
            author.Save();

            author.Books.Add(book1);
            author.Books.Add(book2);

            book1.Authors.Add(author);
            book2.Authors.Add(author);

            book1.Delete();
            Assert.AreEqual(1, author.Books.Collection().Count());
            Assert.AreEqual(book2.Title, author.Books.Collection().First().Title);

            Assert.AreEqual(1, author.Books.Collection().Count());
            Assert.AreEqual(book2.Title, author.Books.Collection().Single().Title);
        }
    }
}
