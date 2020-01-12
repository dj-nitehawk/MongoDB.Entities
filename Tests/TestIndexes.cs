using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Indexes
    {
        [TestMethod]
        public void full_text_search_with_index_returns_correct_result()
        {
            DB.Index<Author>()
              .Option(o => o.Background = false)
              .Key(a => a.Name, KeyType.Text)
              .Key(a => a.Surname, KeyType.Text)
              .Create();

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            author1.Save();

            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            author2.Save();

            var res = DB.SearchText<Author>(author1.Surname);

            Assert.AreEqual(author1.Surname, res.First().Surname);
        }

        [TestMethod]
        public void full_text_search_with_wilcard_text_index_works()
        {
            DB.Index<Author>()
              .Option(o => o.Background = false)
              .Key(a => a, KeyType.Text)
              .Create();

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            author1.Save();

            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            author2.Save();

            var res = DB.SearchText<Author>(author1.Surname);

            Assert.AreEqual(author1.Surname, res.First().Surname);
        }

        [TestMethod]
        public void fuzzy_text_search_with_text_index_works()
        {
            DB.Index<Book>()
              .Option(o => o.Background = false)
              .Key(b => b.Review.Alias, KeyType.Text)
              .Key(b => b.Title, KeyType.Text)
              .Create();

            var a1 = new Book { Title = "One", Review = new Review { Alias = "Katherine Zeta Jones" } };
            var a2 = new Book { Title = "Two", Review = new Review { Alias = "Katheryne Zeta Jones" } };
            var a3 = new Book { Title = "Three", Review = new Review { Alias = "Katheryne Jones Abigale" } };
            var a4 = new Book { Title = "Four", Review = new Review { Alias = "Katheryne Jones Abigale" } };
            var a5 = new Book { Title = "Five", Review = new Review { Alias = "Katya Bykova Jones" } };

            DB.Save(new[] { a1, a2, a3, a4, a5 });

            //todo: search here and assert

            DB.Delete<Book>(new[] { a1.ID, a2.ID, a3.ID, a4.ID, a5.ID });
        }

        [TestMethod]
        public void creating_compound_index_works()
        {
            DB.Index<Book>()
              .Key(x => x.Genres, KeyType.Geo2D)
              .Key(x => x.Title, KeyType.Descending)
              .Key(x => x.ModifiedOn, KeyType.Descending)
              .Option(o => o.Background = true)
              .Create();

            DB.Index<Book>()
              .Key(x => x.Genres, KeyType.Geo2D)
              .Key(x => x.Title, KeyType.Descending)
              .Key(x => x.ModifiedOn, KeyType.Ascending)
              .Option(o => o.Background = true)
              .Create();

            DB.Index<Author>()
              .Key(x => x.Age, KeyType.Hashed)
              .Create();

            DB.Index<Author>()
                .Key(x => x.Age, KeyType.Ascending)
                .Create();

            DB.Index<Author>()
                .Key(x => x.Age, KeyType.Descending)
                .Create();
        }

    }
}
