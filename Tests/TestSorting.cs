using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Sorting
    {
        [TestMethod]
        public void sorting_lists_by_levenshtein_distance_works()
        {
            var books = new[] {
                new Book { Title = "One", Review = new Review { Alias = "one two three four five six seven" } },
                new Book { Title = "Two", Review = new Review { Alias = "one two three four five six" } },
                new Book { Title = "Three", Review = new Review { Alias = "one two three four five" } },
                new Book { Title = "Four", Review = new Review { Alias = "one two three four" } },
                new Book { Title = "Five", Review = new Review { Alias = "one two three" } }
            };

            var res = books.SortByRelevance("One TWO Three", b => b.Review.Alias);

            Assert.AreEqual(5, res.Count());
            Assert.AreEqual("Five", res.First().Title);
            Assert.AreEqual("One", res.Last().Title);
        }

        [TestMethod]
        public void sorting_lists_by_levenshtein_distance_specify_max_distance()
        {
            var books = new[] {
                new Book { Title = "One", Review = new Review { Alias = "one two three four five six seven" } },
                new Book { Title = "Two", Review = new Review { Alias = "one two three four five six" } },
                new Book { Title = "Three", Review = new Review { Alias = "one two three four five" } },
                new Book { Title = "Four", Review = new Review { Alias = "one two three four" } },
                new Book { Title = "Five", Review = new Review { Alias = "one two three" } }
            };

            var res = books.SortByRelevance("One TWO Three", b => b.Review.Alias, 10).ToArray();

            Assert.AreEqual(3, res.Length);
            Assert.AreEqual("Five", res[0].Title);
            Assert.AreEqual("Four", res[1].Title);
            Assert.AreEqual("Three", res[2].Title);
        }
    }
}
