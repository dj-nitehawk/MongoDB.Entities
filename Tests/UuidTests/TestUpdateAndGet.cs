using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class UpdateAndGetUuid
{
    [TestMethod]
    public async Task updating_modifies_correct_documents()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorUuid { Name = "bumcda1", Surname = "surname1" };
        await author1.SaveAsync();
        var author2 = new AuthorUuid { Name = "bumcda2", Surname = guid };
        await author2.SaveAsync();
        var author3 = new AuthorUuid { Name = "bumcda3", Surname = guid };
        await author3.SaveAsync();

        var res = await DB.Default.UpdateAndGet<AuthorUuid, string>()
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
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorUuid { Name = "bumcda1", Surname = "surname1", Age = 1 };
        await author1.SaveAsync();
        var author2 = new AuthorUuid { Name = "bumcda2", Surname = guid, Age = 1 };
        await author2.SaveAsync();
        var author3 = new AuthorUuid { Name = "bumcda3", Surname = guid, Age = 1 };
        await author3.SaveAsync();

        var res = await DB.Default.UpdateAndGet<AuthorUuid>()
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
        var guid = Guid.NewGuid().ToString();

        var author = new AuthorUuid { Name = "uwput", Surname = guid, Age = 666 };
        await author.SaveAsync();

        var pipeline = new Template<AuthorUuid>(
                           @"
            [
              { $set: { <FullName>: { $concat: ['$<Name>',' ','$<Surname>'] } } },
              { $unset: '<Age>'}
            ]")
                       .Path(a => a.FullName!)
                       .Path(a => a.Name)
                       .Path(a => a.Surname)
                       .Path(a => a.Age);

        var res = await DB.Default.UpdateAndGet<AuthorUuid>()
                          .Match(a => a.ID == author.ID)
                          .WithPipeline(pipeline)
                          .ExecutePipelineAsync();

        Assert.AreEqual(author.Name + " " + author.Surname, res!.FullName);
    }

    [TestMethod]
    public async Task update_with_aggregation_pipeline_works()
    {
        var guid = Guid.NewGuid().ToString();

        var author = new AuthorUuid { Name = "uwapw", Surname = guid };
        await author.SaveAsync();

        var stage = new Template<AuthorUuid>("{ $set: { <FullName>: { $concat: ['$<Name>','-','$<Surname>'] } } }")
                    .Path(a => a.FullName!)
                    .Path(a => a.Name)
                    .Path(a => a.Surname)
                    .RenderToString();

        var res = await DB.Default.UpdateAndGet<AuthorUuid>()
                          .Match(a => a.ID == author.ID)
                          .WithPipelineStage(stage)
                          .ExecutePipelineAsync();

        Assert.AreEqual(author.Name + "-" + author.Surname, res!.FullName);
    }

    [TestMethod]
    public async Task update_with_array_filters_using_templates_work()
    {
        var guid = Guid.NewGuid().ToString();
        var book = new BookUuid
        {
            Title = "uwafw " + guid,
            OtherAuthors = new[]
            {
                new AuthorUuid
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorUuid
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorUuid
                {
                    Name = "name",
                    Age = 100
                }
            }
        };
        await book.SaveAsync();

        var filters = new Template<AuthorUuid>(
                          @"
            [
                { '<a.Age>': { $gte: <age> } },
                { '<b.Name>': 'name' }
            ]")
                      .Elements(0, author => author.Age)
                      .Tag("age", "120")
                      .Elements(1, author => author.Name);

        var update = new Template<BookUuid>(
                         @"
            { $set: { 
                '<OtherAuthors.$[a].Age>': <age>,
                '<OtherAuthors.$[b].Name>': '<value>'
              } 
            }")
                     .PosFiltered(b => b.OtherAuthors[0].Age)
                     .PosFiltered(b => b.OtherAuthors[1].Name)
                     .Tag("age", "321")
                     .Tag("value", "updated");

        var res = await DB.Default.UpdateAndGet<BookUuid>()
                          .Match(b => b.ID == book.ID)
                          .WithArrayFilters(filters)
                          .Modify(update)
                          .ExecuteAsync();

        Assert.AreEqual(321, res!.OtherAuthors[0].Age);
    }

    [TestMethod]
    public async Task update_with_array_filters_work()
    {
        var guid = Guid.NewGuid().ToString();
        var book = new BookUuid
        {
            Title = "uwafw " + guid,
            OtherAuthors = new[]
            {
                new AuthorUuid
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorUuid
                {
                    Name = "name",
                    Age = 123
                },
                new AuthorUuid
                {
                    Name = "name",
                    Age = 100
                }
            }
        };
        await book.SaveAsync();

        var arrFil = new Template<AuthorUuid>("{ '<a.Age>': { $gte: <age> } }")
                     .Elements(0, author => author.Age)
                     .Tag("age", "120");

        var prop1 = new Template<BookUuid>("{ $set: { '<OtherAuthors.$[a].Age>': <age> } }")
                    .PosFiltered(b => b.OtherAuthors[0].Age)
                    .Tag("age", "321")
                    .RenderToString();

        var filt2 = Prop.Elements<AuthorUuid>(1, a => a.Name);
        var prop2 = Prop.PosFiltered<BookUuid>(b => b.OtherAuthors[1].Name);

        var res = await DB.Default.UpdateAndGet<BookUuid>()
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
        var book = new BookUuid();

        var lastNum = await book.NextSequentialNumberAsync();

        Parallel.For(1, 11, _ => book.NextSequentialNumberAsync().GetAwaiter().GetResult());

        Assert.AreEqual(lastNum + 10, await book.NextSequentialNumberAsync() - 1);
    }

    [TestMethod]
    public async Task next_sequential_number_for_entities_multidb()
    {
        await DB.InitAsync("mongodb-entities-test-multi");

        var book = new BookUuid();

        var lastNum = await book.NextSequentialNumberAsync();

        Parallel.For(1, 11, _ => book.NextSequentialNumberAsync().GetAwaiter().GetResult());

        Assert.AreEqual(lastNum + 10, await book.NextSequentialNumberAsync() - 1);
    }

    [TestMethod]
    public async Task on_before_update_for_updateandget()
    {
        var db = new MyDBUuid();

        var flower = new FlowerUuid { Name = "flower" };
        await db.SaveAsync(flower);
        Assert.AreEqual("God", flower.CreatedBy);

        var res = await db
                        .UpdateAndGet<FlowerUuid>()
                        .MatchID(flower.Id)
                        .ModifyWith(flower)
                        .ExecuteAsync();

        Assert.AreEqual("Human", res!.UpdatedBy);
    }
}