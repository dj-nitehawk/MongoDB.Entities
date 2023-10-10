using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DistinctUuid
{
    [TestMethod]
    public async Task distinct_works()
    {
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var guids = new[] { guid1, guid2 };

        await new[] {
            new AuthorUuid{ Name = guid1 },
            new AuthorUuid{ Name = guid1 },
            new AuthorUuid{ Name = guid2 },
            new AuthorUuid{ Name = guid2 },
        }.SaveAsync();

        var res = await DB.Distinct<AuthorUuid, string>()
            .Match(a => guids.Contains(a.Name))
            .Property(a => a.Name)
            .ExecuteAsync();

        Assert.AreEqual(2, res.Count);
        Assert.IsTrue(!res.Except(guids).Any());
    }
}
