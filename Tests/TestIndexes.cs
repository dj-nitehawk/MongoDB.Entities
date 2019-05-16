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
            DB.DefineIndex<Author>(
                Type.Text,
                Priority.Foreground,
                x => x.Name,
                x => x.Surname);

            var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            author1.Save();

            var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            author2.Save();

            var res = DB.SearchText<Author>(author1.Surname);

            Assert.AreEqual(author1.Surname, res.First().Surname);
        }

        [TestMethod]
        public void creating_indexes_work()
        {
            DB.DefineIndex<Author>(
                Type.Descending,
                Priority.Foreground,
                x => x.Surname,
                x => x.Age,
                x => x.Surname);

            DB.DefineIndex<Book>(
                Type.Ascending,
                Priority.Foreground,
                x => x.Title);
        }
    }
}
