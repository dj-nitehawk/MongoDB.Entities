using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MongoDB.Entities.Tests;

[TestClass]
public class SortingUuid
{
    [TestMethod]
    public void sorting_lists_by_levenshtein_distance_works()
    {
        var books = new[] {
            new BookUuid { Title = "One", Review = new ReviewUuid { Fuzzy = new("one two three four five six seven") } },
            new BookUuid { Title = "Two", Review = new ReviewUuid { Fuzzy = new("one two three four five six") } },
            new BookUuid { Title = "Three", Review = new ReviewUuid { Fuzzy = new("one two three four five") } },
            new BookUuid { Title = "Four", Review = new ReviewUuid { Fuzzy = new("one two three four") } },
            new BookUuid { Title = "Five", Review = new ReviewUuid { Fuzzy = new("one two three") } }
        };

        var res = books.SortByRelevance("One TWO Three", b => b.Review.Fuzzy!.Value!);

        Assert.AreEqual(5, res.Count());
        Assert.AreEqual("Five", res.First().Title);
        Assert.AreEqual("One", res.Last().Title);
    }

    [TestMethod]
    public void sorting_lists_by_levenshtein_distance_specify_max_distance()
    {
        var books = new[] {
            new BookUuid { Title = "One", Review = new ReviewUuid { Fuzzy = new("one two three four five six seven") } },
            new BookUuid { Title = "Two", Review = new ReviewUuid { Fuzzy = new("one two three four five six") } },
            new BookUuid { Title = "Three", Review = new ReviewUuid { Fuzzy = new("one two three four five") } },
            new BookUuid { Title = "Four", Review = new ReviewUuid { Fuzzy = new("one two three four") } },
            new BookUuid { Title = "Five", Review = new ReviewUuid { Fuzzy = new("one two three") } }
        };

        var res = books.SortByRelevance("One TWO Three", b => b.Review.Fuzzy!.Value!, 10).ToArray();

        Assert.AreEqual(3, res.Length);
        Assert.AreEqual("Five", res[0].Title);
        Assert.AreEqual("Four", res[1].Title);
        Assert.AreEqual("Three", res[2].Title);
    }
}
