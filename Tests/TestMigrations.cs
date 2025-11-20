using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class Migrations
{
    [TestMethod]
    public async Task renaming_and_undoing_a_field()
    {
        await DB.Default.MigrateAsync();

        var count = await DB.Default.Collection<Migration>().CountDocumentsAsync (DB.Default.Filter<Migration>().Empty);

        Assert.AreEqual(2, count);

        await DB.Default.Collection<Migration>().Database.DropCollectionAsync("_migration_history_");
    }

    [TestMethod]
    public async Task migrations_work_with_supplied_type_for_discovery()
    {
        await DB.Default.MigrateAsync<Migrations>();

        var count = await DB.Default.Collection<Migration>().CountDocumentsAsync (DB.Default.Filter<Migration>().Empty);

        Assert.AreEqual(2, count);

        await DB.Default.Collection<Migration>().Database.DropCollectionAsync("_migration_history_");
    }
}
