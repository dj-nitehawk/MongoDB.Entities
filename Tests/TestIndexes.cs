using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Indexes
    {
        [TestMethod]
        public async Task full_text_search_with_index_returns_correct_result()
        {
            await DB.Index<Author>()
              .Option(o => o.Background = false)
              .Key(a => a.Name, KeyType.Text)
              .Key(a => a.Surname, KeyType.Text)
              .CreateAsync();

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            await author1.SaveAsync();

            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            await author2.SaveAsync();

            var res = DB.FluentTextSearch<Author>(Search.Full, author1.Surname).ToList();
            Assert.AreEqual(author1.Surname, res[0].Surname);

            var res2 = await DB.Find<Author>()
                         .Match(Search.Full, author1.Surname)
                         .ExecuteAsync();
            Assert.AreEqual(author1.Surname, res2[0].Surname);
        }

        [TestMethod]
        public async Task full_text_search_with_wilcard_text_index_works()
        {
            await DB.Index<Author>()
              .Option(o => o.Background = false)
              .Key(a => a, KeyType.Text)
              .CreateAsync();

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            await author1.SaveAsync();

            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            await author2.SaveAsync();

            var res = await DB.FluentTextSearch<Author>(Search.Full, author1.Surname).ToListAsync();

            Assert.AreEqual(author1.Surname, res[0].Surname);
        }

        [TestMethod]
        public async Task fuzzy_text_search_with_text_index_works()
        {
            await DB.Index<Book>()
              .Option(o => o.Background = false)
              .Key(b => b.Review.Alias, KeyType.Text)
              .Key(b => b.Title, KeyType.Text)
              .CreateAsync();

            var b1 = new Book { Title = "One", Review = new Review { Alias = "Katherine Zeta Jones" } };
            var b2 = new Book { Title = "Two", Review = new Review { Alias = "Katheryne Zeta Jones" } };
            var b3 = new Book { Title = "Three", Review = new Review { Alias = "Katheryne Jones Abigale" } };
            var b4 = new Book { Title = "Four", Review = new Review { Alias = "Katheryne Jones Abigale" } };
            var b5 = new Book { Title = "Five", Review = new Review { Alias = "Katya Bykova Jhohanes" } };
            var b6 = new Book { Title = "Five", Review = new Review { Alias = " " } };

            await DB.SaveAsync(new[] { b1, b2, b3, b4, b5, b6 });

            var res = await DB.Find<Book>()
                        .Match(Search.Fuzzy, "catherine jones")
                        .Project(b => new Book { ID = b.ID, Title = b.Title })
                        .SortByTextScore()
                        .Skip(0)
                        .Limit(6)
                        .ExecuteAsync();

            await DB.DeleteAsync<Book>(new[] { b1.ID, b2.ID, b3.ID, b4.ID, b5.ID, b6.ID });

            Assert.AreEqual(4, res.Count);
            Assert.IsFalse(res.Select(b => b.ID).Contains(b5.ID));
        }

        [TestMethod]
        public async Task sort_by_meta_text_score_dont_retun_the_score()
        {
            await DB.Index<Genre>()
              .Key(g => g.Name, KeyType.Text)
              .Option(o => o.Background = false)
              .CreateAsync();

            var guid = Guid.NewGuid();

            var list = new[] {
                new Genre{ GuidID = guid, Position = 0, Name = "this should not match"},
                new Genre{ GuidID = guid, Position = 3, Name = "one two three four five six"},
                new Genre{ GuidID = guid, Position = 4, Name = "one two three four five six seven"},
                new Genre{ GuidID = guid, Position = 2, Name = "one two three four five six seven eight"},
                new Genre{ GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine"}
            };

            await list.SaveAsync();

            var res = await DB.Find<Genre>()
                        .Match(Search.Full, "one eight nine")
                        .Project(p => new Genre { Name = p.Name, Position = p.Position })
                        .SortByTextScore()
                        .ExecuteAsync();

            await list.DeleteAllAsync();

            Assert.AreEqual(4, res.Count);
            Assert.AreEqual(1, res[0].Position);
            Assert.AreEqual(4, res.Last().Position);
        }

        [TestMethod]
        public async Task sort_by_meta_text_score_retun_the_score()
        {
            await DB.Index<Genre>()
              .Key(g => g.Name, KeyType.Text)
              .Option(o => o.Background = false)
              .CreateAsync();

            var guid = Guid.NewGuid();

            var list = new[] {
                new Genre{ GuidID = guid, Position = 0, Name = "this should not match"},
                new Genre{ GuidID = guid, Position = 3, Name = "one two three four five six"},
                new Genre{ GuidID = guid, Position = 4, Name = "one two three four five six seven"},
                new Genre{ GuidID = guid, Position = 2, Name = "one two three four five six seven eight"},
                new Genre{ GuidID = guid, Position = 1, Name = "one two three four five six seven eight nine"}
            };

            await list.SaveAsync();

            var res = await DB.Find<Genre>()
                        .Match(Search.Full, "one eight nine")
                        .SortByTextScore(g => g.SortScore)
                        .Sort(g => g.Position, Order.Ascending)
                        .ExecuteAsync();

            await list.DeleteAllAsync();

            Assert.AreEqual(4, res.Count);
            Assert.AreEqual(1, res[0].Position);
            Assert.AreEqual(4, res.Last().Position);
            Assert.IsTrue(res[0].SortScore > 0);
        }

        [TestMethod]
        public async Task creating_compound_index_works()
        {
            await DB.Index<Book>()
              .Key(x => x.Genres, KeyType.Geo2D)
              .Key(x => x.Title, KeyType.Descending)
              .Key(x => x.ModifiedOn, KeyType.Descending)
              .Option(o => o.Background = true)
              .CreateAsync();

            await DB.Index<Book>()
              .Key(x => x.Genres, KeyType.Geo2D)
              .Key(x => x.Title, KeyType.Descending)
              .Key(x => x.ModifiedOn, KeyType.Ascending)
              .Option(o => o.Background = true)
              .CreateAsync();

            await DB.Index<Author>()
              .Key(x => x.Age, KeyType.Hashed)
              .CreateAsync();

            await DB.Index<Author>()
                .Key(x => x.Age, KeyType.Ascending)
                .CreateAsync();

            await DB.Index<Author>()
                .Key(x => x.Age, KeyType.Descending)
                .CreateAsync();
        }
    }
}
