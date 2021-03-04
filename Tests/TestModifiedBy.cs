using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class ModifiedBy
    {
        [TestMethod]
        public async Task throw_if_mod_by_not_supplied()
        {
            var db = new DBContext();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await db.SaveAsync(new Author()));
        }

        [TestMethod]
        public async Task base_mod_by_save()
        {
            string userID = ObjectId.GenerateNewId().ToString();

            var db = new DBContext(
                modifiedBy: new Entities.ModifiedBy
                {
                    UserID = userID,
                    UserName = "TestUser"
                });

            var author = new Author();
            await db.SaveAsync(author);

            var res = await db.Find<Author>().OneAsync(author.ID);

            Assert.AreEqual(res.UpdatedBy.UserID, userID);
            Assert.AreEqual(res.UpdatedBy.UserName, "TestUser");
        }

        [TestMethod]
        public async Task derived_mod_by_save()
        {
            string userID = ObjectId.GenerateNewId().ToString();

            var db = new DBContext(
                modifiedBy: new UpdatedBy
                {
                    UserID = userID,
                    UserName = "TestUser",
                    UserType = "TEST"
                });

            var author = new Book();
            await db.SaveAsync(author);

            var res = await db.Find<Book>().OneAsync(author.ID);

            Assert.AreEqual(res.ModifiedBy.UserID, userID);
            Assert.AreEqual(res.ModifiedBy.UserName, "TestUser");
            Assert.AreEqual(res.ModifiedBy.UserType, "TEST");
        }

        [TestMethod]
        public async Task mod_by_update()
        {
            string userID = ObjectId.GenerateNewId().ToString();
            var db = new DBContext(
                modifiedBy: new UpdatedBy
                {
                    UserID = userID,
                    UserName = "TestUser",
                    UserType = "TEST"
                });
            var author = new Book();
            await db.SaveAsync(author);

            userID = ObjectId.GenerateNewId().ToString();
            db.ModifiedBy = new UpdatedBy
            {
                UserID = userID,
                UserName = "TestUserUPDATED",
                UserType = "TEST-UPDATED"
            };
            await db
                .Update<Book>()
                .MatchID(author.ID)
                .Modify(b => b.Title, "TEST().BOOK")
                .ExecuteAsync();

            var res = await db.Find<Book>().OneAsync(author.ID);

            Assert.AreEqual(res.ModifiedBy.UserID, userID);
            Assert.AreEqual(res.ModifiedBy.UserName, "TestUserUPDATED");
            Assert.AreEqual(res.ModifiedBy.UserType, "TEST-UPDATED");
            Assert.AreEqual(res.Title, "TEST().BOOK");
        }
    }
}
