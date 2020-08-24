using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Migrations
    {
        [TestMethod]
        public async Task renaming_and_undoing_a_field()
        {
            await DB.MigrateAsync();

            var count = await DB.Collection<Migration>().CountDocumentsAsync(DB.Filter<Migration>().Empty);

            Assert.AreEqual(2, count);

            await DB.Collection<Migration>().Database.DropCollectionAsync("_migration_history_");
        }

        [TestMethod]
        public async Task migrations_work_with_supplied_type_for_discovery()
        {
            await DB.MigrateAsync<Migrations>();

            var count = await DB.Collection<Migration>().CountDocumentsAsync(DB.Filter<Migration>().Empty);

            Assert.AreEqual(2, count);

            await DB.Collection<Migration>().Database.DropCollectionAsync("_migration_history_");
        }
    }
}
