using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class CappedCollection
{
    readonly string _dbName = "mongodb-entities-capped-collection-tests";
    readonly DB _db1;

    public CappedCollection()
    {
        _db1 = DB.InitAsync(_dbName).GetAwaiter().GetResult();
    }
    
    [TestMethod]
    public async Task test_creating_a_capped_collection()
    {
        await _db1.CreateCollectionAsync<Auto>(o => {
            o.Capped = true;
            o.MaxSize = 1048576;
        });
    }


}