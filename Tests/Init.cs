using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Entities.Tests.Models;

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

        //ID type level generator: all entities with long ID properties
        BsonSerializer.RegisterIdGenerator(typeof(long), new SequentialLongIdGenerator());

        //entity level generators
        DB.RegisterIdGenerator<CustomerWithCustomID>(new CustomerIdGenerator());
        DB.RegisterIdGenerator<CustomIDOverride>(new TicksIdGenerator());
        DB.RegisterIdGenerator<CustomIDDuplicate>(new DuplicateIdGenerator());
        DB.RegisterIdGenerator<CustomStringIdParent>(new PrefixedStringIdGenerator("parent"));
        DB.RegisterIdGenerator<CustomStringIdChild>(new PrefixedStringIdGenerator("child"));

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

        return await DB.InitAsync(databaseName, MongoClientSettings.FromConnectionString("mongodb://admin:password@localhost:27017/?replicaSet=rs0&authSource=admin"));
    }
}