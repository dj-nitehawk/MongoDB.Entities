using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities.Tests;

[TestClass]
public class FuzzyStringTestEntity
{
    readonly DB _db = DB.Default;

    [TestMethod]
    public async Task fuzzystring_type_saving_and_retrieval_worksAsync()
    {
        var guid = Guid.NewGuid().ToString();

        await _db.SaveAsync(new BookEntity { Title = "fstsarw", Review = new() { Fuzzy = guid.ToFuzzy() } });

        var res = await _db.Queryable<BookEntity>()
                           .Where(b => b.Review.Fuzzy!.Value == guid)
                           .SingleAsync();

        Assert.AreEqual(guid, res.Review.Fuzzy!.Value);
    }

    [TestMethod]
    public async Task fuzzystring_type_with_nulls_workAsync()
    {
        var guid = Guid.NewGuid().ToString();

        await _db.SaveAsync(
            new BookEntity
            {
                Title = guid,
                Review = new() { Fuzzy = null }
            });

        var res = await _db.Queryable<BookEntity>()
                           .Where(b => b.Title == guid)
                           .SingleAsync();

        Assert.IsNull(res.Review.Fuzzy?.Value);
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