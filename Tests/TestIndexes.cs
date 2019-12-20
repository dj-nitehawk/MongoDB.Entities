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
