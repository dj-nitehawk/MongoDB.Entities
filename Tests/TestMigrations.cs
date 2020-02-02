using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Migrations
    {
        [TestMethod]
        public void renaming_and_undoing_a_field()
        {
            DB.Migrate();

            var count = DB.Collection<Migration>().CountDocuments(DB.Filter<Migration>().Empty);

            Assert.AreEqual(2, count);

            DB.Collection<Migration>().Database.DropCollection("_migration_history_");
        }

        [TestMethod]
        public void migrations_work_with_supplied_type_for_discovery()
        {
            DB.Migrate<Migrations>();

            var count = DB.Collection<Migration>().CountDocuments(DB.Filter<Migration>().Empty);

            Assert.AreEqual(2, count);

            DB.Collection<Migration>().Database.DropCollection("_migration_history_");
        }
    }
}
