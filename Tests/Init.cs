using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

[assembly: DoNotParallelize]

namespace MongoDB.Entities.Tests;

[TestClass]
public static class InitTest
{
    public static MongoClientSettings ClientSettings1 { get; set; }
    public static MongoClientSettings ClientSettings2 { get; set; }
    public static bool UseTestContainers;

    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        UseTestContainers = Environment.GetEnvironmentVariable("MONGODB_ENTITIES_TESTCONTAINERS") != null;

        if (UseTestContainers)
        {
            var testContainer1 = await TestDatabase.CreateDatabase();
            ClientSettings1 = MongoClientSettings.FromConnectionString(testContainer1.GetConnectionString());
            var testContainer2 = await TestDatabase.CreateDatabase();
            ClientSettings2 = MongoClientSettings.FromConnectionString(testContainer2.GetConnectionString());
        }

        await InitTestDatabase("mongodb-entities-test");
    }

    public static async Task<DB> InitTestDatabase(string databaseName)
    {
        if (UseTestContainers)
        {
            await DB.InitAsync(databaseName, ClientSettings2);

            return await DB.InitAsync(databaseName, ClientSettings1);
        }

        return await DB.InitAsync(databaseName);
    }
}