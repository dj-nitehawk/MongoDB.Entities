using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Threading.Tasks;

namespace Benchmark;

public abstract class BenchBase
{
    const string DBName = "mongodb-entities-benchmark";
    protected static IMongoCollection<Author> AuthorCollection { get; }
    protected static IMongoCollection<Book> BookCollection { get; }
    protected static IMongoDatabase Database { get; }

    static BenchBase()
    {
        var useTestContainers = Environment.GetEnvironmentVariable("MONGODB_ENTITIES_TESTCONTAINERS") != null;

        if (useTestContainers)
        {
            var testContainer = TestDatabase.CreateDatabase().GetAwaiter().GetResult();
            var clientSettings = MongoClientSettings.FromConnectionString(testContainer.GetConnectionString());
            DB.InitAsync(DBName, clientSettings).GetAwaiter().GetResult();
        }
        else
            DB.InitAsync(DBName).GetAwaiter().GetResult();

        var dbInstance = DB.Default;

        dbInstance.Database().Client.DropDatabase(DBName);
        Database = dbInstance.Database();
        AuthorCollection = dbInstance.Collection<Author>();
        BookCollection = dbInstance.Collection<Book>();

        Console.WriteLine();
        Console.WriteLine("SEEDING DATA...");
    }

    public abstract Task MongoDB_Entities();

    public abstract Task Official_Driver();
}