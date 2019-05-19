using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

            var index = new Index<Author>();
            index.Key(a => a.Surname, Type.Text);
            index.Key(a => a.Name, Type.Text);


        }
    }

    /// <summary>
    /// Defines and creates an index.
    /// <para>TIP: Define the keys first with .Key(...) method and finally call the .Define() method.</para>
    /// </summary>
    /// <typeparam name="T">Any class that inherits from Entity</typeparam>
    public class Index<T> where T : Entity
    {
        internal List<Key<T>> Keys { get; set; }

        public void Define() {
            //todo: move internals here
        }
    }

    internal class Key<T> where T : Entity
    {
        public Expression<Func<T, object>> Property { get; set; }
        public Type Type { get; set; }

        public Key(Expression<Func<T, object>> prop, Type type)
        {
            Property = prop;
            Type = type;
        }
    }

    public static class IndexExtensions
    {
        /// <summary>
        /// Add a key to the index definition
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="propertyToIndex">x => x.Prop1</param>
        /// <param name="type">The type of the key</param>
        public static void Key<T>(this Index<T> index, Expression<Func<T, object>> propertyToIndex, Type type) where T : Entity
        {
            index.Keys.Add(new Key<T>(propertyToIndex, type));
        }
    }
}
