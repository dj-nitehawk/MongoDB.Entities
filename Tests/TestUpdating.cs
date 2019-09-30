using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Updating
    {
        [TestMethod]
        public void updating_modifies_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; author1.Save();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; author3.Save();

            DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .Execute();

            var count = author1.Queryable().Where(a => a.Name == guid && a.Surname == author1.Name).Count();
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void update_by_def_builder_mods_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; author1.Save();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; author3.Save();

            DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(b => b.Inc(a => a.Age, 10))
              .Modify(b => b.Set(a => a.Name, guid))
              .Modify(b => b.CurrentDate(a => a.ModifiedOn))
              .Execute();

            var res = DB.Find<Author>().Many(a => a.Surname == guid && a.Age == 10);

            Assert.AreEqual(2, res.Count());
            Assert.AreEqual(guid, res.First().Name);
        }

        [TestMethod]
        public void nested_properties_update_correctly()
        {
            var guid = Guid.NewGuid().ToString();

            var book = new Book
            {
                Title = "mnpuc title " + guid,
                Review = new Review { Rating = 10.10 }
            };
            book.Save();

            DB.Update<Book>()
                .Match(b => b.Review.Rating == 10.10)
                .Modify(b => b.Review.Rating, 22.22)
                .Execute();

            var res = DB.Find<Book>().One(book.ID);

            Assert.AreEqual(22.22, res.Review.Rating);
        }

        [TestMethod]
        public void bulk_update_modifies_correct_documents()
        {
            var title = "bumcd " + Guid.NewGuid().ToString();
            var books = new Collection<Book>();

            for (int i = 1; i <= 5; i++)
            {
                books.Add(new Book { Title = title, SellingPrice = i });
            }
            books.Save();

            var bulk = DB.Update<Book>();

            foreach (var book in books)
            {
                bulk.Match(b => b.ID == book.ID)
                    .Modify(b => b.SellingPrice, 100)
                    .AddToQueue();
            }

            bulk.Execute();

            var res = DB.Find<Book>()
                        .Many(b => b.Title == title);

            Assert.AreEqual(5, res.Count());
            Assert.AreEqual(5, res.Where(b => b.SellingPrice == 100).Count());
        }

        [TestMethod]
        public void update_with_aggregation_pipeline_works()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = "uwapw", Surname = guid };
            author.Save();

            DB.Update<Author>()
              .Match(a => a.ID == author.ID)
              .WithPipelineStage($"{{ $set: {{ {nameof(author.FullName)}: {{ $concat: ['${nameof(author.Name)}','-','${nameof(author.Surname)}'] }} }} }}")
              .ExecutePipeline();

            var fullname = DB.Find<Author>().One(author.ID).FullName;
            Assert.AreEqual(author.Name + "-" + author.Surname, fullname);
        }

        [TestMethod]
        public void update_with_array_filters_work()
        {
            var guid = Guid.NewGuid().ToString();
            var book = new Book
            {
                Title = "uwafw " + guid,
                OtherAuthors = new[]
                {
                    new Author{
                        Name ="name",
                        Age = 123
                    },
                    new Author{
                        Name ="name",
                        Age = 123
                    },
                    new Author{
                        Name ="name",
                        Age = 100
                    },
                }
            };
            book.Save();

            var filt1 = Prop.Elements<Author>(0, a => a.Age);
            var prop1 = Prop.PosFiltered<Book>(b => b.OtherAuthors[0].Age);

            var filt2 = Prop.Elements<Author>(1, a => a.Name);
            var prop2 = Prop.PosFiltered<Book>(b => b.OtherAuthors[1].Name);

            DB.Update<Book>()

              .Match(b => b.ID == book.ID)

              .WithArrayFilter("{'" + filt1 + "':{$gte:120}}")
              .Modify("{$set:{'" + prop1 + "':321}}")

              .WithArrayFilter("{'" + filt2 + "':'name'}")
              .Modify("{$set:{'" + prop2 + "':'updated'}}")

              .Execute();

            var res = DB.Queryable<Book>()
                        .Where(b => b.ID == book.ID)
                        .SelectMany(b => b.OtherAuthors)
                        .ToList();

            Assert.AreEqual(2, res.Count(a => a.Age == 321));
            Assert.AreEqual(3, res.Count(a => a.Name == "updated"));
        }
    }
}
