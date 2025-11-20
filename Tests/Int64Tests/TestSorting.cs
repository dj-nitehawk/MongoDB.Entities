using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class SortingInt64
{
    [TestMethod]
    public void sorting_lists_by_levenshtein_distance_works()
    {
        var books = new[]
        {
            new BookInt64 { Title = "One", Review = new() { Fuzzy = new("one two three four five six seven") } },
            new BookInt64 { Title = "Two", Review = new() { Fuzzy = new("one two three four five six") } },
            new BookInt64 { Title = "Three", Review = new() { Fuzzy = new("one two three four five") } },
            new BookInt64 { Title = "Four", Review = new() { Fuzzy = new("one two three four") } },
            new BookInt64 { Title = "Five", Review = new() { Fuzzy = new("one two three") } }
        };

        var res = books.SortByRelevance("One TWO Three", b => b.Review.Fuzzy!.Value);

        Assert.AreEqual(5, res.Count());
        Assert.AreEqual("Five", res.First().Title);
        Assert.AreEqual("One", res.Last().Title);
    }

    [TestMethod]
    public void sorting_lists_by_levenshtein_distance_specify_max_distance()
    {
        var books = new[]
        {
            new BookInt64 { Title = "One", Review = new() { Fuzzy = new("one two three four five six seven") } },
            new BookInt64 { Title = "Two", Review = new() { Fuzzy = new("one two three four five six") } },
            new BookInt64 { Title = "Three", Review = new() { Fuzzy = new("one two three four five") } },
            new BookInt64 { Title = "Four", Review = new() { Fuzzy = new("one two three four") } },
            new BookInt64 { Title = "Five", Review = new() { Fuzzy = new("one two three") } }
        };

        var res = books.SortByRelevance("One TWO Three", b => b.Review.Fuzzy!.Value, 10).ToArray();

        Assert.AreEqual(3, res.Length);
        Assert.AreEqual("Five", res[0].Title);
        Assert.AreEqual("Four", res[1].Title);
        Assert.AreEqual("Three", res[2].Title);
    }
}