using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public static class InitTest
{
    static MongoClientSettings ClientSettings { get; set; }
    static bool _useTestContainers;

    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        _useTestContainers = System.Environment.GetEnvironmentVariable("MONGODB_ENTITIES_TESTCONTAINERS") != null;

        if (_useTestContainers)
        {
            var testContainer = await TestDatabase.CreateDatabase();
            ClientSettings = MongoClientSettings.FromConnectionString(testContainer.GetConnectionString());
        }

        await InitTestDatabase("mongodb-entities-test");
    }

    public static async Task<DB> InitTestDatabase(string databaseName)
    {
        if (_useTestContainers)
            return await DB.InitAsync(databaseName, ClientSettings);

        return await DB.InitAsync(databaseName);
    }
}