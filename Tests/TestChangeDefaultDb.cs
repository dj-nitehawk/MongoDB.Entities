using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class DefaultDatabaseSwitching
    {
        [TestMethod]
        public void throw_argument_null_exception()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DB.ChangeDefaultDatabase(""));
        }

        [TestMethod]
        public void throw_invalid_operation_exception()
        {
            Assert.ThrowsException<InvalidOperationException>(() => DB.ChangeDefaultDatabase("db3"));
        }

        [TestMethod]
        public async Task returns_correct_database()
        {
            await DB.InitAsync("test1");
            await DB.InitAsync("test2");

            var database = DB.Database("test2");

            DB.ChangeDefaultDatabase("test2");

            var bookDb = DB.Database<Book>();

            Assert.AreEqual(database, bookDb);
        }
        
        //[TestMethod]
        public async Task do_not_change_default_database_when_the_same()
        {
            await DB.InitAsync("test1");

            var defaultDb = DB.Database(default);

            DB.ChangeDefaultDatabase("mongodb-entities-test");

            var bookDb = DB.Database<Book>();
            Assert.AreSame(defaultDb, bookDb);
        }
    }
}