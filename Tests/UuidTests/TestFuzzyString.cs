using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class FuzzyStringTesUuid
{
    [TestMethod]
    public async Task fuzzystring_type_saving_and_retrieval_worksAsync()
    {
        var guid = Guid.NewGuid().ToString();

        await new BookUuid { Title = "fstsarw", Review = new ReviewUuid { Fuzzy = guid.ToFuzzy() } }.SaveAsync();

        var res = await DB.Queryable<BookUuid>()
                    .Where(b => b.Review.Fuzzy!.Value == guid)
                    .SingleAsync();

        Assert.AreEqual(guid, res.Review.Fuzzy!.Value);
    }

    [TestMethod]
    public async Task fuzzystring_type_with_nulls_workAsync()
    {
        var guid = Guid.NewGuid().ToString();

        await new BookUuid { Title = guid, Review = new ReviewUuid { Fuzzy = null! } }.SaveAsync();

        var res = await DB.Queryable<BookUuid>()
                    .Where(b => b.Title == guid)
                    .SingleAsync();

        Assert.AreEqual(null, res.Review.Fuzzy?.Value);
    }

    [TestMethod]
    public void double_metaphone_removes_diacritics()
    {
        var istanbul = "İstanbul".ToDoubleMetaphoneHash();
        Assert.AreEqual("ASTN", istanbul);

        var cremeBrulee = "Crème Brûlée".ToDoubleMetaphoneHash();
        Assert.AreEqual("KRM PRL", cremeBrulee);
    }
}
