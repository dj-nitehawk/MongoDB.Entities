using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class TestMultiClient
{
    readonly string _dbName = "mongodb-entities-test-multi-client";
    readonly DB _db1;
    readonly DB _db2;

    public TestMultiClient()
    {
        _db1 = DB.InitAsync(_dbName, InitTest.ClientSettings1).GetAwaiter().GetResult();
        _db2 = DB.InitAsync(_dbName, InitTest.ClientSettings2).GetAwaiter().GetResult();
    }
    
    [TestMethod]
    public async Task test_two_clients_with_same_db_name()
    {
        if (InitTest.UseTestContainers)
        {
            // These should be the same dbname in different clients
            Assert.AreNotEqual(_db1, _db2);
            
            var auto1 = new Auto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020
            };
            await auto1.SaveAsync(_db1);
            Assert.IsNotNull(auto1.ID);
            
            var auto2 = new Auto
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2021
            };
            await auto2.SaveAsync(_db2);
            Assert.IsNotNull(auto2.ID);
            
            Assert.AreNotEqual(auto1.ID, auto2.ID);
            
            var res1 = await _db1.Find<Auto>().MatchID(auto1.ID).ExecuteSingleAsync();
            Assert.IsNotNull(res1);
            Assert.AreEqual(auto1.Make, res1.Make);
            Assert.AreEqual(auto1.Model, res1.Model);
            Assert.AreEqual(auto1.Year, res1.Year);

            var res2 = await _db2.Find<Auto>().MatchID(auto2.ID).ExecuteSingleAsync();
            Assert.IsNotNull(res2);
            Assert.AreEqual(auto2.Make, res2.Make);
            Assert.AreEqual(auto2.Model, res2.Model);
            Assert.AreEqual(auto2.Year, res2.Year);

            res1 = await _db1.Find<Auto>().MatchID(auto2.ID).ExecuteSingleAsync();
            res2 = await _db2.Find<Auto>().MatchID(auto1.ID).ExecuteSingleAsync();
            
            Assert.IsNull(res1);
            Assert.IsNull(res2);
        }
    }


}