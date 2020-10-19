using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Deleting
    {
        [TestMethod]
        public async Task delete_by_id_removes_entity_from_collectionAsync()
        {
            var author1 = new Author { Name = "auth1" };
            var author2 = new Author { Name = "auth2" };
            var author3 = new Author { Name = "auth3" };

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
            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "derarti1" };
            var book2 = new Book { Title = "derarti2" };

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
            var book = new Book { Title = "Test" }; await book.SaveAsync();
            var author1 = new Author { Name = "ewtrcd1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "ewtrcd2" }; await author2.SaveAsync();
            await book.GoodAuthors.AddAsync(author1);
            book.OtherAuthors = (new Author[] { author1, author2 });
            await book.SaveAsync();
            await book.OtherAuthors.DeleteAllAsync();
            Assert.AreEqual(0, await book.GoodAuthors.ChildrenQueryable().CountAsync());
            Assert.AreEqual(null, await author1.Queryable().Where(a => a.ID == author1.ID).SingleOrDefaultAsync());
        }

        [TestMethod]
        public async Task deleting_a_one2many_ref_entity_makes_parent_nullAsync()
        {
            var book = new Book { Title = "Test" }; await book.SaveAsync();
            var author = new Author { Name = "ewtrcd1" }; await author.SaveAsync();
            book.MainAuthor = author.ToReference();
            await book.SaveAsync();
            await author.DeleteAsync();
            Assert.AreEqual(null, await book.MainAuthor.ToEntityAsync());
        }

        [TestMethod]
        public async Task delete_by_expression_deletes_all_matchesAsync()
        {
            var author1 = new Author { Name = "xxx" }; await author1.SaveAsync();
            var author2 = new Author { Name = "xxx" }; await author2.SaveAsync();

            await DB.DeleteAsync<Author>(x => x.Name == "xxx");

            var count = await DB.Queryable<Author>()
                          .CountAsync(a => a.Name == "xxx");

            Assert.AreEqual(0, count);
        }

        //[TestMethod]
        public async Task high_volume_deletes_with_idsAsync()
        {
            var IDs = new List<string>(100100);

            for (int i = 0; i < 100100; i++)
            {
                IDs.Add(ObjectId.GenerateNewId().ToString());
            }

            await DB.DeleteAsync<Blank>(IDs);
        }

        [TestCategory("SkipWhenLiveUnitTesting")]
        //[TestMethod]
        public async Task high_volume_deletes_with_expressionAsync()
        {
            //start with clean collection
            await DB.DropCollectionAsync<Blank>();

            var list = new List<Blank>(100100);
            for (int i = 0; i < 100100; i++)
            {
                list.Add(new Blank());
            }
            await list.SaveAsync();

            Assert.AreEqual(100100, DB.Queryable<Blank>().Count());

            await DB.DeleteAsync<Blank>(_ => true);

            Assert.AreEqual(0, await DB.CountAsync<Blank>());

            //reclaim disk space
            await DB.DropCollectionAsync<Blank>();
            await DB.SaveAsync(new Blank());
        }
    }
}
