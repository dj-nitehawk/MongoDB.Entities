using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Relationships
    {
        [TestMethod]
        public void setting_one_to_one_reference_returns_correct_entity()
        {
            var book = new Book { Title = "book" };
            var author = new Author { Name = "sotorrce" };
            author.Save();
            book.MainAuthor = author.ToReference();
            book.Save();
            var res = book.Queryable()
                          .Where(b => b.ID == book.ID)
                          .Single()
                          .MainAuthor.ToEntity();
            Assert.AreEqual(author.Name, res.Name);
        }

        [TestMethod]
        public void setting_one_to_one_reference_with_implicit_operator_returns_correct_entity()
        {
            var book = new Book { Title = "book" };
            var author = new Author { Name = "sotorrce" };
            author.Save();
            book.MainAuthor = author.ID;
            book.Save();
            var res = book.Queryable()
                          .Where(b => b.ID == book.ID)
                          .Single()
                          .MainAuthor.ToEntity();
            Assert.AreEqual(author.Name, res.Name);
        }

        [TestMethod]
        public void one_to_one_to_entity_with_lambda_projection()
        {
            var book = new Book { Title = "book" };
            var author = new Author { Name = "ototoewlp" };
            author.Save();
            book.MainAuthor = author.ToReference();
            book.Save();
            var res = book.Queryable()
                          .Where(b => b.ID == book.ID)
                          .Single()
                          .MainAuthor.ToEntity(a => new Author { Name = a.Name });
            Assert.AreEqual(author.Name, res.Name);
            Assert.AreEqual(null, res.ID);
        }

        [TestMethod]
        public void one_to_one_to_entity_with_mongo_projection()
        {
            var book = new Book { Title = "book" };
            var author = new Author { Name = "ototoewmp" };
            author.Save();
            book.MainAuthor = author.ToReference();
            book.Save();
            var res = book.Queryable()
                          .Where(b => b.ID == book.ID)
                          .Single()
                          .MainAuthor.ToEntity(p => p.Include(a => a.Name).Exclude(a => a.ID));
            Assert.AreEqual(author.Name, res.Name);
            Assert.AreEqual(null, res.ID);
        }

        [TestMethod]
        public void adding_one2many_references_returns_correct_entities_queryable()
        {
            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "aotmrrceb1" };
            var book2 = new Book { Title = "aotmrrceb2" };
            book1.Save(); book2.Save();
            author.Save();
            author.Books.Add(book1);
            author.Books.Add(book2);
            var books = author.Queryable()
                              .Where(a => a.ID == author.ID)
                              .Single()
                              .Books
                              .ChildrenQueryable().ToArray();
            Assert.AreEqual(book2.Title, books[1].Title);
        }

        [TestMethod]
        public void adding_one2many_references_returns_correct_entities_fluent()
        {
            var author = new Author { Name = "author" };
            var book1 = new Book { Title = "aotmrrcebf1" };
            var book2 = new Book { Title = "aotmrrcebf2" };
            book1.Save(); book2.Save();
            author.Save();
            author.Books.Add(book1);
            author.Books.Add(book2);
            var books = author.Queryable()
                              .Where(a => a.ID == author.ID)
                              .Single()
                              .Books
                              .ChildrenFluent().ToList();
            Assert.AreEqual(book2.Title, books[1].Title);
        }

        [TestMethod]
        public void removing_a_one2many_ref_removes_correct_entities()
        {
            var book = new Book { Title = "rotmrrceb" };
            book.Save();
            var author1 = new Author { Name = "rotmrrcea1" }; author1.Save();
            var author2 = new Author { Name = "rotmrrcea2" }; author2.Save();
            book.GoodAuthors.Add(author1);
            book.GoodAuthors.Add(author2);
            var remAuthor = DB.Queryable<Author>()
                              .Where(a => a.ID == author2.ID)
                              .Single();
            book.GoodAuthors.Remove(remAuthor);
            var resBook = book.Queryable()
                              .Where(b => b.ID == book.ID)
                              .Single();
            Assert.AreEqual(1, resBook.GoodAuthors.ChildrenQueryable().Count());
            Assert.AreEqual(author1.Name, resBook.GoodAuthors.ChildrenQueryable().First().Name);
        }

        [TestMethod]
        public void collection_shortcut_of_many_returns_correct_children()
        {
            var book = new Book { Title = "book" };
            book.Save();
            var author1 = new Author { Name = "csomrcc1" }; author1.Save();
            var author2 = new Author { Name = "csomrcc1" }; author2.Save();
            book.GoodAuthors.Add(author1);
            book.GoodAuthors.Add(author2);
            Assert.AreEqual(2, book.GoodAuthors.ChildrenQueryable().Count());
            Assert.AreEqual(author1.Name, book.GoodAuthors.ChildrenQueryable().First().Name);
        }

        [TestMethod]
        public void accessing_coll_shortcut_on_unsaved_parent_throws()
        {
            var book = new Book { Title = "acsoupt" };
            Assert.ThrowsException<InvalidOperationException>(() => book.GoodAuthors.ChildrenQueryable().Count());
        }

        [TestMethod]
        public void adding_many2many_returns_correct_children()
        {
            var book1 = new Book { Title = "ac2mrceb1" }; book1.Save();
            var book2 = new Book { Title = "ac2mrceb2" }; book2.Save();

            var gen1 = new Genre { Name = "ac2mrceg1" }; gen1.Save();
            var gen2 = new Genre { Name = "ac2mrceg1" }; gen2.Save();

            book1.Genres.Add(gen1);
            book1.Genres.Add(gen2);
            book1.Genres.Add(gen1);
            Assert.AreEqual(2, DB.Queryable<Book>().Where(b => b.ID == book1.ID).Single().Genres.ChildrenQueryable().Count());
            Assert.AreEqual(gen1.Name, book1.Genres.ChildrenQueryable().First().Name);

            gen1.Books.Add(book1);
            gen1.Books.Add(book2);
            gen1.Books.Add(book1);
            Assert.AreEqual(2, gen1.Queryable().Where(g => g.ID == gen1.ID).Single().Books.ChildrenQueryable().Count());
            Assert.AreEqual(gen1.Name, book2.Queryable().Where(b => b.ID == book2.ID).Single().Genres.ChildrenQueryable().First().Name);

            gen2.Books.Add(book1);
            gen2.Books.Add(book2);
            Assert.AreEqual(2, book1.Genres.ChildrenQueryable().Count());
            Assert.AreEqual(gen2.Name, book2.Queryable().Where(b => b.ID == book2.ID).Single().Genres.ChildrenQueryable().First().Name);
        }

        [TestMethod]
        public void removing_many2many_returns_correct_children()
        {
            var book1 = new Book { Title = "rm2mrceb1" }; book1.Save();
            var book2 = new Book { Title = "rm2mrceb2" }; book2.Save();

            var gen1 = new Genre { Name = "rm2mrceg1" }; gen1.Save();
            var gen2 = new Genre { Name = "rm2mrceg1" }; gen2.Save();

            book1.Genres.Add(gen1);
            book1.Genres.Add(gen2);
            book2.Genres.Add(gen1);
            book2.Genres.Add(gen2);

            book1.Genres.Remove(gen1);
            Assert.AreEqual(1, book1.Genres.ChildrenQueryable().Count());
            Assert.AreEqual(gen2.Name, book1.Genres.ChildrenQueryable().Single().Name);
            Assert.AreEqual(1, gen1.Books.ChildrenQueryable().Count());
            Assert.AreEqual(book2.Title, gen1.Books.ChildrenQueryable().First().Title);
        }

        [TestMethod]
        public void getting_parents_of_a_relationship_queryable_works()
        {
            var guid = Guid.NewGuid().ToString();

            var book = new Book { Title = "Planet Of The Apes " + guid };
            book.Save();

            var genre = new Genre { Name = "SciFi " + guid };
            genre.Save();

            var genre1 = new Genre { Name = "Thriller " + guid };
            genre1.Save();

            book.Genres.Add(genre);
            book.Genres.Add(genre1);

            var books = book.Genres
                            .ParentsQueryable<Book>(genre.ID)
                            .ToArray();

            Assert.AreEqual(1, books.Count());
            Assert.AreEqual(book.Title, books.Single().Title);

            books = book.Genres
                    .ParentsQueryable<Book>(genre.Queryable().Where(g => g.Name.Contains(guid)))
                    .ToArray();

            Assert.AreEqual(1, books.Count());
            Assert.AreEqual(book.Title, books.Where(b => b.ID == book.ID).Single().Title);

            var genres = genre.Books
                              .ParentsQueryable<Genre>(new[] { book.ID, book.ID })
                              .ToArray();

            Assert.AreEqual(2, genres.Count());
            Assert.AreEqual(genre.Name, genres.Where(g => g.ID == genre.ID).First().Name);

            genres = genre.Books
                     .ParentsQueryable<Genre>(book.Queryable().Where(b => b.ID == book.ID))
                     .ToArray();

            Assert.AreEqual(2, genres.Count());
            Assert.IsTrue(genres.Any(g => g.ID == genre.ID));
        }

        [TestMethod]
        public void getting_parents_of_a_relationship_fluent_works()
        {

            var guid = Guid.NewGuid().ToString();

            var book = new Book { Title = "Planet Of The Apes " + guid };
            book.Save();

            var genre = new Genre { Name = "SciFi " + guid };
            genre.Save();

            var genre1 = new Genre { Name = "Thriller " + guid };
            genre1.Save();

            book.Genres.Add(genre);
            book.Genres.Add(genre1);

            var books = book.Genres
                            .ParentsFluent<Book>(genre.ID)
                            .ToList();

            Assert.AreEqual(1, books.Count());
            Assert.AreEqual(book.Title, books.Single().Title);

            books = book.Genres
                            .ParentsFluent<Book>(genre.Fluent().Match(g => g.Name.Contains(guid)))
                            .ToList();

            Assert.AreEqual(1, books.Count());
            Assert.AreEqual(book.Title, books.Single().Title);

            var genres = genre.Books
                              .ParentsFluent<Genre>(new[] { book.ID })
                              .ToList();

            Assert.AreEqual(2, genres.Count());
            Assert.AreEqual(genre.Name, genres.Where(g => g.ID == genre.ID).Single().Name);

            genres = genre.Books
                    .ParentsFluent<Genre>(book.Fluent().Match(b => b.ID == book.ID))
                    .ToList();

            Assert.AreEqual(1, books.Count());
            Assert.AreEqual(book.Title, books.Single().Title);

        }
    }
}
