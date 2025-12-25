using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class UpdateAndGetEntity
{
    [TestMethod]
    public async Task updating_modifies_correct_documents()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = "bumcda1", Surname = "surname1" };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "bumcda2", Surname = guid };
        await db.SaveAsync(author2);
        var author3 = new AuthorEntity { Name = "bumcda3", Surname = guid };
        await db.SaveAsync(author3);

        var res = await db.UpdateAndGet<AuthorEntity, string>()
                          .Match(a => a.Surname == guid)
                          .Modify(a => a.Name, guid)
                          .Modify(a => a.Surname, author1.Name)
                          .Option(o => o.MaxTime = TimeSpan.FromSeconds(10))
                          .Project(a => a.Name)
                          .ExecuteAsync();

        Assert.AreEqual(guid, res);
    }

    [TestMethod]
    public async Task update_by_def_builder_mods_correct_docs()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = "bumcda1", Surname = "surname1", Age = 1 };
        await db.SaveAsync(author1);
        var author2 = new AuthorEntity { Name = "bumcda2", Surname = guid, Age = 1 };
        await db.SaveAsync(author2);
        var author3 = new AuthorEntity { Name = "bumcda3", Surname = guid, Age = 1 };
        await db.SaveAsync(author3);

        var res = await db.UpdateAndGet<AuthorEntity>()
                          .Match(a => a.Surname == guid)
                          .Modify(b => b.Inc(a => a.Age, 1))
                          .Modify(b => b.Set(a => a.Name, guid))
                          .Modify(b => b.CurrentDate(a => a.ModifiedOn))
                          .ExecuteAsync();

        Assert.AreEqual(2, res!.Age);
    }

    [TestMethod]
    public async Task update_with_pipeline_using_template()
    {
        var db = DB.Default.WithModifiedBy(new());
        var guid = Guid.NewGuid().ToString();

        var author = new AuthorEntity { Name = "uwput", Surname = guid, Age = 666 };
        await db.SaveAsync(author);

        var pipeline = new Template<AuthorEntity>(
                           """
                           [
                             { $set: { <FullName>: { $concat: ['$<Name>',' ','$<Surname>'] } } },
                             { $unset: '<Age>'}
                           ]
                           """)
                       .Path(a => a.FullName!)
                       .Path(a => a.Name)
                       .Path(a => a.Surname)
                       .Path(a => a.Age);

        var res = await db.UpdateAndGet<AuthorEntity>()
                          .Match(a => a.ID == author.ID)
                          .WithPipeline(pipeline)
                          .ExecutePipelineAsync();

        Assert.AreEqual(author.Name + " " + author.Surname, res!.FullName);
    }

    [TestMethod]
    public async Task update_with_aggregation_pipeline_works()
    {
        var db = DB.Default.WithModifiedBy(new());
        var guid = Guid.NewGuid().ToString();

        var author = new AuthorEntity { Name = "uwapw", Surname = guid };
        await db.SaveAsync(author);

        var stage = new Template<AuthorEntity>("{ $set: { <FullName>: { $concat: ['$<Name>','-','$<Surname>'] } } }")
                    .Path(a => a.FullName)
                    .Path(a => a.Name)
                    .Path(a => a.Surname)
                    .RenderToString();

        var res = await db.UpdateAndGet<AuthorEntity>()
                          .Match(a => a.ID == author.ID)
                          .WithPipelineStage(stage)
                          .ExecutePipelineAsync();

        Assert.AreEqual(author.Name + "-" + author.Surname, res!.FullName);
    }

    [TestMethod]
    public async Task update_with_array_filters_using_templates_work()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var book = new BookEntity
        {
            Title = "uwafw " + guid,
            OtherAuthors = new[]
            {
                new AuthorEntity
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorEntity
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorEntity
                {
                    Name = "name",
                    Age = 100
                }
            }
        };
        await db.SaveAsync(book);

        var filters = new Template<AuthorEntity>(
                          """
                          [
                              { '<a.Age>': { $gte: <age> } },
                              { '<b.Name>': 'name' }
                          ]
                          """)
                      .Elements(0, author => author.Age)
                      .Tag("age", "120")
                      .Elements(1, author => author.Name);

        var update = new Template<BookEntity>(
                         """
                         { $set: { 
                             '<OtherAuthors.$[a].Age>': <age>,
                             '<OtherAuthors.$[b].Name>': '<value>'
                           } 
                         }
                         """)
                     .PosFiltered(b => b.OtherAuthors[0].Age)
                     .PosFiltered(b => b.OtherAuthors[1].Name)
                     .Tag("age", "321")
                     .Tag("value", "updated");

        var res = await db.UpdateAndGet<BookEntity>()
                          .Match(b => b.ID == book.ID)
                          .WithArrayFilters(filters)
                          .Modify(update)
                          .ExecuteAsync();

        Assert.AreEqual(321, res!.OtherAuthors[0].Age);
    }

    [TestMethod]
    public async Task update_with_array_filters_work()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();
        var book = new BookEntity
        {
            Title = "uwafw " + guid,
            OtherAuthors = new[]
            {
                new AuthorEntity
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorEntity
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorEntity
                {
                    Name = "name",
                    Age = 100
                }
            }
        };
        await db.SaveAsync(book);

        var arrFil = new Template<AuthorEntity>("{ '<a.Age>': { $gte: <age> } }")
                     .Elements(0, author => author.Age)
                     .Tag("age", "120");

        var prop1 = new Template<BookEntity>("{ $set: { '<OtherAuthors.$[a].Age>': <age> } }")
                    .PosFiltered(b => b.OtherAuthors[0].Age)
                    .Tag("age", "321")
                    .RenderToString();

        var filt2 = Prop.Elements<AuthorEntity>(1, a => a.Name);
        var prop2 = Prop.PosFiltered<BookEntity>(b => b.OtherAuthors[1].Name);

        var res = await db.UpdateAndGet<BookEntity>()
                          .Match(b => b.ID == book.ID)
                          .WithArrayFilter(arrFil)
                          .Modify(prop1)
                          .WithArrayFilter("{'" + filt2 + "':'name'}")
                          .Modify("{$set:{'" + prop2 + "':'updated'}}")
                          .ExecuteAsync();

        Assert.AreEqual(321, res!.OtherAuthors[0].Age);
    }

    [TestMethod]
    public async Task next_sequential_number_for_entities()
    {
        var db = DB.Default;

        var lastNum = await db.NextSequentialNumberAsync<BookEntity>();

        await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (_, ct) => await db.NextSequentialNumberAsync<BookEntity>());

        Assert.AreEqual(lastNum + 10, await db.NextSequentialNumberAsync<BookEntity>() - 1);
    }

    [TestMethod]
    public async Task next_sequential_number_for_entities_multidb()
    {
        var dbName = "mongodb-entities-test-multi";
        await DB.InitAsync(dbName);
        var db = DB.Instance(dbName);

        var lastNum = await db.NextSequentialNumberAsync<BookEntity>();

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 10),
            async (_, ct) =>
            {
                await db.NextSequentialNumberAsync<BookEntity>(ct);
            });

        Assert.AreEqual(lastNum + 10, await db.NextSequentialNumberAsync<BookEntity>() - 1);
    }

    [TestMethod]
    public async Task on_before_update_for_updateandget()
    {
        var db = new MyDBEntity();

        var flower = new FlowerEntity { Name = "flower" };
        await db.SaveAsync(flower);
        Assert.AreEqual("God", flower.CreatedBy);

        var res = await db.UpdateAndGet<FlowerEntity>()
                          .MatchID(flower.Id)
                          .ModifyWith(flower)
                          .ExecuteAsync();

        Assert.AreEqual("Human", res!.UpdatedBy);
    }
}