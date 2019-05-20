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
            new Index<Author>()
                .Options(o => o.Background = false)
                .Key(a => a.Name, Type.Text)
                .Key(a => a.Surname, Type.Text)
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
            new Index<Book>()
                .Key(x => x.AllGenres, Type.Geo2D)
                .Key(x => x.Title, Type.Descending)
                .Options(o => o.Background = true)
                .Create();

            new Index<Author>()
                .Key(x => x.Age, Type.Hashed)
                .Create();
        }
    }

    //todo: create indexes for ref collections on db init (do benchmark without index to compare)
}
