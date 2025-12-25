using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DistinctEntity
{
    [TestMethod]
    public async Task distinct_works()
    {
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var guids = new[] { guid1, guid2 };

        var db = DB.Default.WithModifiedBy(new());

        await db.SaveAsync(
        [
            new() { Name = guid1 },
            new() { Name = guid1 },
            new() { Name = guid2 },
            new AuthorEntity { Name = guid2 }
        ]);

        var res = await db.Distinct<AuthorEntity, string>()
                          .Match(a => guids.Contains(a.Name))
                          .Property(a => a.Name)
                          .ExecuteAsync();

        Assert.HasCount(2, res);
        Assert.IsFalse(res.Except(guids).Any());
    }
}