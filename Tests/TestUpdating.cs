using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Update
    {
        [TestMethod]
        public async Task updating_modifies_correct_documents()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; await author3.SaveAsync();

            await DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .ExecuteAsync();

            var count = author1.Queryable().Where(a => a.Name == guid && a.Surname == author1.Name).Count();
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void update_without_filter_throws()
        {
            Assert.ThrowsException<ArgumentException>(() => DB.Update<Author>().Modify(a => a.Age2, 22).ExecuteAsync().GetAwaiter().GetResult());
        }

        [TestMethod]
        public async Task updating_returns_correct_result()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; await author3.SaveAsync();

            var res = await DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(a => a.Name, guid)
              .Modify(a => a.Surname, author1.Name)
              .Option(o => o.BypassDocumentValidation = true)
              .ExecuteAsync();

            Assert.AreEqual(2, res.MatchedCount);
            Assert.AreEqual(2, res.ModifiedCount);
        }

        [TestMethod]
        public async Task update_by_def_builder_mods_correct_docs()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = "bumcda1", Surname = "surname1" }; await author1.SaveAsync();
            var author2 = new Author { Name = "bumcda2", Surname = guid }; await author2.SaveAsync();
            var author3 = new Author { Name = "bumcda3", Surname = guid }; await author3.SaveAsync();

            await DB.Update<Author>()
              .Match(a => a.Surname == guid)
              .Modify(b => b.Inc(a => a.Age, 10))
              .Modify(b => b.Set(a => a.Name, guid))
              .Modify(b => b.CurrentDate(a => a.ModifiedOn))
              .ExecuteAsync();

            var res = await DB.Find<Author>().ManyAsync(a => a.Surname == guid && a.Age == 10);

            Assert.AreEqual(2, res.Count);
            Assert.AreEqual(guid, res[0].Name);
        }

        [TestMethod]
        public async Task nested_properties_update_correctly()
        {
            var guid = Guid.NewGuid().ToString();

            var book = new Book
            {
                Title = "mnpuc title " + guid,
                Review = new Review { Rating = 10.10 }
            };
            await book.SaveAsync();

            await DB.Update<Book>()
                .Match(b => b.Review.Rating == 10.10)
                .Modify(b => b.Review.Rating, 22.22)
                .ExecuteAsync();

            var res = await DB.Find<Book>().OneAsync(book.ID);

            Assert.AreEqual(22.22, res.Review.Rating);
        }

        [TestMethod]
        public async Task bulk_update_modifies_correct_documents()
        {
            var title = "bumcd " + Guid.NewGuid().ToString();
            var books = new Collection<Book>();

            for (int i = 1; i <= 5; i++)
            {
                books.Add(new Book { Title = title, Price = i });
            }
            await books.SaveAsync();

            var bulk = DB.Update<Book>();

            foreach (var book in books)
            {
                bulk.Match(b => b.ID == book.ID)
                    .Modify(b => b.Price, 100)
                    .AddToQueue();
            }

            await bulk.ExecuteAsync();

            var res = await DB.Find<Book>()
                        .ManyAsync(b => b.Title == title);

            Assert.AreEqual(5, res.Count);
            Assert.AreEqual(5, res.Count(b => b.Price == 100));
        }

        [TestMethod]
        public async Task update_with_pipeline_using_template()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = "uwput", Surname = guid, Age = 666 };
            await author.SaveAsync();

            var pipeline = new Template<Author>(@"
            [
              { $set: { <FullName>: { $concat: ['$<Name>',' ','$<Surname>'] } } },
              { $unset: '<Age>'}
            ]")
                .Path(a => a.FullName)
                .Path(a => a.Name)
                .Path(a => a.Surname)
                .Path(a => a.Age);

            await DB.Update<Author>()
              .Match(a => a.ID == author.ID)
              .WithPipeline(pipeline)
              .ExecutePipelineAsync();

            var res = await DB.Find<Author>().OneAsync(author.ID);

            Assert.AreEqual(author.Name + " " + author.Surname, res.FullName);
            Assert.AreEqual(0, res.Age);
        }

        [TestMethod]
        public async Task update_with_aggregation_pipeline_works()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = "uwapw", Surname = guid };
            await author.SaveAsync();

            var stage = new Template<Author>("{ $set: { <FullName>: { $concat: ['$<Name>','-','$<Surname>'] } } }")
                .Path(a => a.FullName)
                .Path(a => a.Name)
                .Path(a => a.Surname)
                .RenderToString();

            await DB.Update<Author>()
              .Match(a => a.ID == author.ID)
              .WithPipelineStage(stage)
              .ExecutePipelineAsync();

            var fullname = (await DB.Find<Author>().OneAsync(author.ID)).FullName;
            Assert.AreEqual(author.Name + "-" + author.Surname, fullname);
        }

        [TestMethod]
        public async Task update_with_template_match()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = "uwtm", Surname = guid };
            await author.SaveAsync();

            var filter = new Template(@"
            { 
                _id: ObjectId('<ID>') 
            }")
                .Tag("ID", author.ID);

            var stage = new Template<Author>("[{ $set: { <FullName>: { $concat: ['$<Name>','-','$<Surname>'] } } }]")
                .Path(a => a.FullName)
                .Path(a => a.Name)
                .Path(a => a.Surname);

            await DB.Update<Author>()
              .Match(filter)
              .WithPipeline(stage)
              .ExecutePipelineAsync();

            var fullname = (await DB.Find<Author>()
                             .OneAsync(author.ID))
                             .FullName;

            Assert.AreEqual(author.Name + "-" + author.Surname, fullname);
        }

        [TestMethod]
        public async Task update_with_array_filters_using_templates_work()
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
            await book.SaveAsync();

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

            await DB.Update<Book>()

              .Match(b => b.ID == book.ID)

              .WithArrayFilters(filters)
              .Modify(update)

              .ExecuteAsync();

            var res = DB.Queryable<Book>()
                        .Where(b => b.ID == book.ID)
                        .SelectMany(b => b.OtherAuthors)
                        .ToList();

            Assert.AreEqual(2, res.Count(a => a.Age == 321));
            Assert.AreEqual(3, res.Count(a => a.Name == "updated"));
        }

        [TestMethod]
        public async Task update_with_array_filters_work()
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
            await book.SaveAsync();

            var arrFil = new Template<Author>("{ '<a.Age>': { $gte: <age> } }")
                                .Elements(0, author => author.Age)
                                .Tag("age", "120");

            var prop1 = new Template<Book>("{ $set: { '<OtherAuthors.$[a].Age>': <age> } }")
                                .PosFiltered(b => b.OtherAuthors[0].Age)
                                .Tag("age", "321")
                                .RenderToString();

            var filt2 = Prop.Elements<Author>(1, a => a.Name);
            var prop2 = Prop.PosFiltered<Book>(b => b.OtherAuthors[1].Name);

            await DB.Update<Book>()

              .Match(b => b.ID == book.ID)

              .WithArrayFilter(arrFil)
              .Modify(prop1)

              .WithArrayFilter("{'" + filt2 + "':'name'}")
              .Modify("{$set:{'" + prop2 + "':'updated'}}")

              .ExecuteAsync();

            var res = DB.Queryable<Book>()
                        .Where(b => b.ID == book.ID)
                        .SelectMany(b => b.OtherAuthors)
                        .ToList();

            Assert.AreEqual(2, res.Count(a => a.Age == 321));
            Assert.AreEqual(3, res.Count(a => a.Name == "updated"));
        }

        [TestMethod]
        public async Task skip_setting_mod_date_if_user_is_doing_something_with_it()
        {
            var book = new Book { Title = "test" };
            await book.SaveAsync();

            book = await DB.Find<Book>().OneAsync(book.ID);
            Assert.IsTrue(DateTime.UtcNow.Subtract(book.ModifiedOn).TotalSeconds < 5);

            var targetDate = DateTime.UtcNow.AddDays(100);

            await DB
                .Update<Book>()
                .MatchID(book.ID)
                .Modify(b => b.ModifiedOn, targetDate)
                .ExecuteAsync();

            book = await DB.Find<Book>().OneAsync(book.ID);
            Assert.AreEqual(targetDate.ToShortDateString(), book.ModifiedOn.ToShortDateString());
        }

        [TestMethod]
        public async Task update_with_modifyonly_works()
        {
            var book = new Book
            {
                Title = "book",
                Price = 100,
                PublishedOn = DateTime.UtcNow
            };

            await book.SaveAsync();

            book.Title = "updated";
            book.Price = 200;
            book.PublishedOn = null;

            await DB.Update<Book>()
                .MatchID(book.ID)
                .ModifyOnly(x => new { x.Title, x.PublishedOn }, book)
                .ExecuteAsync();

            var res = await DB.Find<Book>().OneAsync(book.ID);

            Assert.AreEqual(res.Title, "updated");
            Assert.AreEqual(res.Price, 100);
            Assert.AreEqual(res.PublishedOn, null);
        }

        [TestMethod]
        public async Task update_with_modifywith_works()
        {
            var flower = new Flower
            {
                Color = "red",
                Name = "lilly"
            };
            await flower.SaveAsync();

            flower.Color = "green";
            flower.Name = "daisy";

            await DB.Update<Flower>()
                .MatchID(flower.ID)
                .ModifyWith(flower)
                .ExecuteAsync();

            var res = await DB.Find<Flower>().OneAsync(flower.ID);

            Assert.AreEqual("green", res.Color);
            Assert.AreEqual("daisy", res.Name);
        }

        [TestMethod]
        public async Task bulk_update_with_modifywith()
        {
            var books = new[] {
                new Book{ Title ="one"},
                new Book{ Title ="two"},
            };

            await books.SaveAsync();

            foreach (var book in books)
            {
                await DB
                    .Update<Book>()
                    .MatchID(book.ID)
                    .Modify(b => b.ModifiedOn, DateTime.UtcNow.AddDays(-100))
                    .ExecuteAsync();
            }

            var bulkUpdate = DB.Update<Book>();

            foreach (var book in books)
            {
                book.Title = "updated!";
                bulkUpdate
                    .MatchID(book.ID)
                    .ModifyWith(book)
                    .AddToQueue();
            }

            await bulkUpdate.ExecuteAsync();

            var bIDs = books.Select(b => b.ID).ToArray();

            var res = await DB.Find<Book>()
                .Match(b => bIDs.Contains(b.ID))
                .ExecuteAsync();

            Assert.AreEqual("updated!", res[0].Title);
            Assert.AreEqual("updated!", res[1].Title);
            Assert.IsTrue(res.All(b => b.ModifiedOn.Date == DateTime.UtcNow.Date));
        }

        [TestMethod]
        public async Task update_with_modifyexcept_works()
        {
            var book = new Book
            {
                Title = "book",
                Price = 100,
                PublishedOn = DateTime.UtcNow
            };

            await book.SaveAsync();

            book.Title = "updated";
            book.Price = 200;
            book.PublishedOn = null;

            await DB.Update<Book>()
                .MatchID(book.ID)
                .ModifyExcept(x => new { x.Title, x.PublishedOn }, book)
                .ExecuteAsync();

            var res = await DB.Find<Book>().OneAsync(book.ID);

            Assert.AreEqual(res.Title, "book");
            Assert.AreEqual(res.Price, 200);
            Assert.IsNotNull(res.PublishedOn);
        }

        [TestMethod]
        public async Task modified_on_is_being_set_with_modifyonly()
        {
            var book = new Book { Title = "test" };
            await book.SaveAsync();

            await DB.Update<Book>()
                .MatchID(book.ID)
                .Modify(b => b.ModifiedOn, DateTime.MinValue)
                .ExecuteAsync();

            book.ModifiedOn = DateTime.MinValue;
            book.Title = "updated";
            book.Price = 100;

            await DB.Update<Book>()
                    .MatchID(book.ID)
                    .ModifyOnly(x => new { x.Title, x.ModifiedOn }, book)
                    .ExecuteAsync();

            var res = await DB.Find<Book>().OneAsync(book.ID);

            Assert.AreEqual(res.Title, "updated");
            Assert.AreEqual(0, res.Price);
            Assert.IsTrue(res.ModifiedOn > DateTime.UtcNow.AddSeconds(-10));
        }

        [TestMethod]
        public async Task update_with_global_filter()
        {
            var db = new MyDB();

            var guid = Guid.NewGuid().ToString();

            await new[] {
                new Author { Name = guid, Age = 111},
                new Author { Name = guid, Age = 200},
                new Author { Name = guid, Age = 111},
            }.SaveAsync();

            var res = await db
                .Update<Author>()
                .Match(a => a.Name == guid)
                .Modify(a => a.Surname, "surname")
                .ExecuteAsync();

            Assert.AreEqual(2, res.ModifiedCount);
        }

        [TestMethod]
        public async Task on_before_update_for_update()
        {
            var db = new MyDB();

            var flower = new Flower { Name = "flower" };
            await db.SaveAsync(flower);
            Assert.AreEqual("God", flower.CreatedBy);

            await db.Update<Flower>()
                    .MatchID(flower.ID)
                    .ModifyWith(flower)
                    .ExecuteAsync();

            var res = await db.Find<Flower>().OneAsync(flower.ID);
            Assert.AreEqual("Human", res.UpdatedBy);
        }
    }
}
