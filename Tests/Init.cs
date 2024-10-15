using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public static class InitTest
{
    public static MongoClientSettings ClientSettings { get; set; }
    
    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        var testContainer = await TestDatabase.CreateDatabase();
        ClientSettings = MongoClientSettings.FromConnectionString(testContainer.GetConnectionString());
        await DB.InitAsync("mongodb-entities-test", ClientSettings);
    }
}