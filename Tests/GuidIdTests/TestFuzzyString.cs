using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class FuzzyStringTestGuid
{
    [TestMethod]
    public async Task fuzzystring_type_saving_and_retrieval_worksAsync()
    {
        var guid = Guid.NewGuid().ToString();

        await new BookGuid { Title = "fstsarw", Review = new ReviewGuid { Fuzzy = guid.ToFuzzy() } }.SaveAsync();

        var res = await DB.Queryable<BookGuid>()
                    .Where(b => b.Review.Fuzzy!.Value == guid)
                    .SingleAsync();

        Assert.AreEqual(guid, res.Review.Fuzzy!.Value);
    }

    [TestMethod]
    public async Task fuzzystring_type_with_nulls_workAsync()
    {
        var guid = Guid.NewGuid().ToString();

        await new BookGuid { Title = guid, Review = new ReviewGuid { Fuzzy = null } }.SaveAsync();

        var res = await DB.Queryable<BookGuid>()
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
