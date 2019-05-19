using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Indexes
    {
        [TestMethod]
        public void full_text_search_with_index_returns_correct_result()
        {
            //DB.DefineIndex<Author>(
            //    Type.Text,
            //    Priority.Foreground,
            //    x => x.Name,
            //    x => x.Surname);

            //var author1 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            //author1.Save();

            //var author2 = new Author { Name = "Name", Surname = Guid.NewGuid().ToString() };
            //author2.Save();

            //var res = DB.SearchText<Author>(author1.Surname);

            //Assert.AreEqual(author1.Surname, res.First().Surname);

            var index = new Index<Author>();
                index.Options(o => o.Background = false);
                index.Key(a => a.Surname, Type.Text);
                index.Key(a => a.Name, Type.Text);
                index.Create();


        }

        [TestMethod]
        public void creating_indexes_work()
        {
            //DB.DefineIndex<Author>(
            //    Type.Descending,
            //    new Options { Background = true},
            //    x => x.Surname,
            //    x => x.Age,
            //    x => x.Name);

            //DB.DefineIndex<Book>(
            //    Type.Ascending,
            //    new Options { Background = false},
            //    x => x.Title);

            //DB.DefineIndex<Author>(
            //    Type.Hashed,
            //    Priority.Foreground,
            //    x => x.Name);

            //var author = new Author();
            //var x = author.Key(a => a.Surname, Type.Text);

        }
    }

    //todo: create indexes for ref collections on db init
}
