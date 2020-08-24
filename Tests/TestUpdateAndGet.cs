using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities.Tests.Models;
using System;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class UpdateAndGet
    {
        [TestMethod]
        public void updating_modifies_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; author1.Save();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; author2.Save();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; author3.Save();

            var res = DB.UpdateAndGet<Author, string>()
                        .Match(a => a.Surname == guid)
                        .Modify(a => a.Name, guid)
                        .Modify(a => a.Surname, author1.Name)
                        .Option(o => o.MaxTime = TimeSpan.FromSeconds(10))
                        .Project(a => a.Name)
                        .Execute();

            Assert.AreEqual(guid, res);
        }

        [TestMethod]
        public void update_by_def_builder_mods_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1", Age = 1 }; author1.Save();
            var author2 = new Author { Name = "bumcda2", Surname = guid, Age = 1 }; author2.Save();
            var author3 = new Author { Name = "bumcda3", Surname = guid, Age = 1 }; author3.Save();

            var res = DB.UpdateAndGet<Author>()
                          .Match(a => a.Surname == guid)
                          .Modify(b => b.Inc(a => a.Age, 1))
                          .Modify(b => b.Set(a => a.Name, guid))
                          .Modify(b => b.CurrentDate(a => a.ModifiedOn))
                          .Execute();

            Assert.AreEqual(2, res.Age);
        }

        [TestMethod]
        public void update_with_pipeline_using_template()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = "uwput", Surname = guid, Age = 666 };
            author.Save();

            var pipeline = new Template<Author>(@"
            [
              { $set: { <FullName>: { $concat: ['$<Name>',' ','$<Surname>'] } } },
              { $unset: '<Age>'}
            ]")
                .Path(a => a.FullName)
                .Path(a => a.Name)
                .Path(a => a.Surname)
                .Path(a => a.Age);

            var res = DB.UpdateAndGet<Author>()
                          .Match(a => a.ID == author.ID)
                          .WithPipeline(pipeline)
                          .ExecutePipeline();

            Assert.AreEqual(author.Name + " " + author.Surname, res.FullName);
        }

        [TestMethod]
        public void update_with_aggregation_pipeline_works()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = "uwapw", Surname = guid };
            author.Save();

            var stage = new Template<Author>("{ $set: { <FullName>: { $concat: ['$<Name>','-','$<Surname>'] } } }")
                .Path(a => a.FullName)
                .Path(a => a.Name)
                .Path(a => a.Surname)
                .ToString();

            var res = DB.UpdateAndGet<Author>()
                          .Match(a => a.ID == author.ID)
                          .WithPipelineStage(stage)
                          .ExecutePipeline();

            Assert.AreEqual(author.Name + "-" + author.Surname, res.FullName);
        }

        [TestMethod]
        public void update_with_array_filters_using_templates_work()
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

            var filters = new Template<Author>(@"
            [
                { '<a.Age>': { $gte: <age> } },
                { '<b.Name>': 'name' }
            ]")
                .Elements(0, author => author.Age)
                .Tag("age", "120")
                .Elements(1, author => author.Name);

            var update = new Template<Book>(@"
            { $set: { 
                '<OtherAuthors.$[a].Age>': <age>,
                '<OtherAuthors.$[b].Name>': '<value>'
              } 
            }")
                .PosFiltered(b => b.OtherAuthors[0].Age)
                .PosFiltered(b => b.OtherAuthors[1].Name)
                .Tag("age", "321")
                .Tag("value", "updated");

            var res = DB.UpdateAndGet<Book>()

              .Match(b => b.ID == book.ID)

              .WithArrayFilters(filters)
              .Modify(update)

              .Execute();

            Assert.AreEqual(321, res.OtherAuthors[0].Age);
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

            var arrFil = new Template<Author>("{ '<a.Age>': { $gte: <age> } }")
                                .Elements(0, author => author.Age)
                                .Tag("age", "120");

            var prop1 = new Template<Book>("{ $set: { '<OtherAuthors.$[a].Age>': <age> } }")
                                .PosFiltered(b => b.OtherAuthors[0].Age)
                                .Tag("age", "321")
                                .ToString();

            var filt2 = Prop.Elements<Author>(1, a => a.Name);
            var prop2 = Prop.PosFiltered<Book>(b => b.OtherAuthors[1].Name);

            var res = DB.UpdateAndGet<Book>()

              .Match(b => b.ID == book.ID)

              .WithArrayFilter(arrFil)
              .Modify(prop1)

              .WithArrayFilter("{'" + filt2 + "':'name'}")
              .Modify("{$set:{'" + prop2 + "':'updated'}}")

              .Execute();

            Assert.AreEqual(321, res.OtherAuthors[0].Age);
        }

        [TestMethod]
        public void next_sequential_number_for_entities()
        {
            var book = new Book();

            var lastNum = book.NextSequentialNumber();

            var bookNum = 0ul;
            Parallel.For(1, 11, _ => bookNum = book.NextSequentialNumber());

            Assert.AreEqual(lastNum + 10, book.NextSequentialNumber() - 1);
        }

        [TestMethod]
        public void next_sequential_number_for_entities_multidb()
        {
            var db = new DB("mongodb-entities-test-multi");

            var img = new Image();

            var lastNum = img.NextSequentialNumber();

            var imgNum = 0ul;
            Parallel.For(1, 11, _ => imgNum = img.NextSequentialNumber());

            Assert.AreEqual(lastNum + 10, img.NextSequentialNumber() - 1);
        }
    }
}
