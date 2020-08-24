using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Deleting
    {
        [TestMethod]
        public void delete_by_id_removes_entity_from_collection()
        {
            var author1 = new Author { Name = "auth1" }; author1.Save();
            var author2 = new Author { Name = "auth2" }; author2.Save();
            var author3 = new Author { Name = "auth3" }; author3.Save();

            author2.Delete();

            var a1 = author1.Queryable()
                             .Where(a => a.ID == author1.ID)
                             .SingleOrDefault();
            var a2 = author2.Queryable()
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

            book1.GoodAuthors.Add(author);
            book2.GoodAuthors.Add(author);

            author.Delete();
            Assert.AreEqual(0, book2.GoodAuthors.ChildrenQueryable().Count());

            book1.Delete();
            Assert.AreEqual(0, author.Books.ChildrenQueryable().Count());
        }

        [TestMethod]
        public void deleteall_removes_entity_and_refs_to_itself()
        {
            var book = new Book { Title = "Test" }; book.Save();
            var author1 = new Author { Name = "ewtrcd1" }; author1.Save();
            var author2 = new Author { Name = "ewtrcd2" }; author2.Save();
            book.GoodAuthors.Add(author1);
            book.OtherAuthors = (new Author[] { author1, author2 });
            book.Save();
            book.OtherAuthors.DeleteAll();
            Assert.AreEqual(0, book.GoodAuthors.ChildrenQueryable().Count());
            Assert.AreEqual(null, author1.Queryable().Where(a => a.ID == author1.ID).SingleOrDefault());
        }

        [TestMethod]
        public void deleting_a_one2many_ref_entity_makes_parent_null()
        {
            var book = new Book { Title = "Test" }; book.Save();
            var author = new Author { Name = "ewtrcd1" }; author.Save();
            book.MainAuthor = author.ToReference();
            book.Save();
            author.Delete();
            Assert.AreEqual(null, book.MainAuthor.ToEntity());
        }

        [TestMethod]
        public void delete_by_expression_deletes_all_matches()
        {
            var author1 = new Author { Name = "xxx" }; author1.Save();
            var author2 = new Author { Name = "xxx" }; author2.Save();

            DB.Delete<Author>(x => x.Name == "xxx");

            var count = DB.Queryable<Author>()
                          .Count(a => a.Name == "xxx");

            Assert.AreEqual(0, count);
        }
    }
}
