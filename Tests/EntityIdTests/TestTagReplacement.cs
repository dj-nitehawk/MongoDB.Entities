using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public class TemplatesEntity
{
    [TestMethod]
    public void missing_tags_throws()
    {
        var template = new Template(
                @"[
            {
              $lookup: {
                from: 'users',
                let: { user_id: '$<user_id>' },
                pipeline: [
                  { $match: {
                      $expr: {
                        $and: [ { $eq: [ '$_id', '$$<user_id>' ] },
                                { $eq: [ '$city', '<cityname>' ] }]}}}],
                as: 'user'
              }
            },
            {
              $match: {
                $expr: { $gt: [ { <size>: '<user>' }, 0 ] }
              }
            }]").Tag("size", "$size")
                .Tag("user", "$user")
                .Tag("missing", "blah");

        Assert.ThrowsExactly<InvalidOperationException>(template.RenderToString);
    }

    [TestMethod]
    public void extra_tags_throws()
    {
        var template = new Template(
                @"[
            {
              $lookup: {
                from: 'users',
                let: { user_id: '$<user_id>' },
                pipeline: [
                  { $match: {
                      $expr: {
                        $and: [ { $eq: [ '$_id', '$$<user_id>' ] },
                                { $eq: [ '$city', '<cityname>' ] }]}}}],
                as: 'user'
              }
            },
            {
              $match: {
                $expr: { $gt: [ { <size>: '<user>' }, 0 ] }
              }
            }]").Tag("size", "$size")
                .Tag("user", "$user");

        Assert.ThrowsExactly<InvalidOperationException>(template.RenderToString);
    }

    [TestMethod]
    public void tag_replacement_works()
    {
        var template = new Template(
                           @"
            {
               $match: { '<OtherAuthors.Name>': /<search_term>/is }
            }")
                       .Path<BookEntity>(b => b.OtherAuthors[0].Name)
                       .Tag("search_term", "Eckhart Tolle");

        const string expectation = @"
            {
               $match: { 'OtherAuthors.Name': /Eckhart Tolle/is }
            }";

        Assert.AreEqual(expectation.Trim(), template.RenderToString());
    }

    [TestMethod]
    public void tag_replacement_works_for_collection()
    {
        var template = new Template<AuthorEntity>(
                @"
            {
               $match: { '<BookEntity>': /search_term/is }
            }")
            .Collection<BookEntity>();

        const string expectation = @"
            {
               $match: { 'BookEntity': /search_term/is }
            }";

        Assert.AreEqual(expectation.Trim(), template.RenderToString());
    }

    [TestMethod]
    public void tag_replacement_works_for_property()
    {
        var template = new Template<BookEntity, AuthorEntity>(
                @"
            {
               $match: { '<Name>': /search_term/is }
            }")
            .Property(b => b.OtherAuthors[0].Name);

        const string expectation = @"
            {
               $match: { 'Name': /search_term/is }
            }";

        Assert.AreEqual(expectation.Trim(), template.RenderToString());
    }

    [TestMethod]
    public void tag_replacement_works_for_properties()
    {
        var template = new Template<BookEntity, AuthorEntity>(
                @"
            {
               $match: { 
                    '<Name>': /search_term/is ,
                    '<Age>': /search_term/is 
                }
            }")
            .Properties(
                b => new
                {
                    b.OtherAuthors[0].Name,
                    b.OtherAuthors[0].Age
                });

        const string expectation = @"
            {
               $match: { 
                    'Name': /search_term/is ,
                    'Age': /search_term/is 
                }
            }";

        Assert.AreEqual(expectation.Trim(), template.RenderToString());
    }

    [TestMethod]
    public void tag_replacement_with_new_expression()
    {
        var template = new Template(
                @"
            {
               $match: { 
                    '<OtherAuthors.Name>': /search_term/is,
                    '<OtherAuthors.Age2>: 55',
                    '<ReviewList.Books.Review>: null'
                }
            }")
            .Paths<BookEntity>(
                b => new
                {
                    b.OtherAuthors[0].Name,
                    b.OtherAuthors[1].Age2,
                    b.ReviewList[1].Books[1].Review
                });

        const string expectation = @"
            {
               $match: { 
                    'OtherAuthors.Name': /search_term/is,
                    'OtherAuthors.Age2: 55',
                    'ReviewList.Books.Review: null'
                }
            }";

        Assert.AreEqual(expectation.Trim(), template.RenderToString());
    }

    [TestMethod]
    public async Task tag_replacement_with_db_pipeline()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid, Age = 54 };
        var author2 = new AuthorEntity { Name = guid, Age = 53 };
        await DB.Default.SaveAsync([author1, author2]);

        var pipeline = new Template<AuthorEntity>(
                           @"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                       .Path(a => a.Name)
                       .Tag("author_name", guid)
                       .Path(a => a.Age);

        var results = await DB.Default.PipelineAsync(pipeline);

        Assert.HasCount(2, results);
        Assert.AreEqual(guid, results[0].Name);
        Assert.AreEqual(54, results.Last().Age);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DB.Default.PipelineSingleAsync(pipeline));

        var first = await DB.Default.PipelineFirstAsync(pipeline);

        Assert.IsNotNull(first);
    }

    [TestMethod]
    public async Task tag_replacement_with_global_filter_prepend()
    {
        var db = new MyDbEntity(prepend: true);

        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid, Age = 111 };
        var author2 = new AuthorEntity { Name = guid, Age = 53 };
        await DB.Default.SaveAsync([author1, author2]);

        var pipeline = new Template<AuthorEntity>(
                           @"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                       .Path(a => a.Name)
                       .Tag("author_name", guid)
                       .Path(a => a.Age);

        var results = await (await db.PipelineCursorAsync(pipeline)).ToListAsync();

        Assert.HasCount(1, results);
        Assert.AreEqual(guid, results[0].Name);
        Assert.AreEqual(111, results.Last().Age);
    }

    [TestMethod]
    public async Task tag_replacement_with_global_filter_append()
    {
        var db = new MyDbEntity(prepend: false);

        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid, Age = 111 };
        var author2 = new AuthorEntity { Name = guid, Age = 53 };
        await DB.Default.SaveAsync([author1, author2]);

        var pipeline = new Template<AuthorEntity>(
                           @"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                       .Path(a => a.Name)
                       .Tag("author_name", guid)
                       .Path(a => a.Age);

        var results = await (await db.PipelineCursorAsync(pipeline)).ToListAsync();

        Assert.HasCount(1, results);
        Assert.AreEqual(guid, results[0].Name);
        Assert.AreEqual(111, results.Last().Age);
    }

    [TestMethod]
    public async Task tag_replacement_with_global_filter_append_string_filter()
    {
        var db = new MyDbTemplatesEntity(prepend: false);

        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid, Age = 111 };
        var author2 = new AuthorEntity { Name = guid, Age = 53 };
        await DB.Default.SaveAsync([author1, author2]);

        var pipeline = new Template<AuthorEntity>(
                           @"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                       .Path(a => a.Name)
                       .Tag("author_name", guid)
                       .Path(a => a.Age);

        var results = await (await db.PipelineCursorAsync(pipeline)).ToListAsync();

        Assert.HasCount(1, results);
        Assert.AreEqual(guid, results[0].Name);
        Assert.AreEqual(111, results.Last().Age);
    }

    [TestMethod]
    public async Task tag_replacement_with_global_filter_prepend_string_filter()
    {
        var db = new MyDbTemplatesEntity(prepend: true);

        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorEntity { Name = guid, Age = 111 };
        var author2 = new AuthorEntity { Name = guid, Age = 53 };
        await DB.Default.SaveAsync([author1, author2]);

        var pipeline = new Template<AuthorEntity>(
                           @"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                       .Path(a => a.Name)
                       .Tag("author_name", guid)
                       .Path(a => a.Age);

        var results = await (await db.PipelineCursorAsync(pipeline)).ToListAsync();

        Assert.HasCount(1, results);
        Assert.AreEqual(guid, results[0].Name);
        Assert.AreEqual(111, results.Last().Age);
    }

    [TestMethod]
    public async Task aggregation_pipeline_with_differnt_input_and_output_typesAsync()
    {
        var db = DB.Default;
        var guid = Guid.NewGuid().ToString();

        var author = new AuthorEntity { Name = guid };
        await db.SaveAsync(author);

        var book = new BookEntity
        {
            Title = guid,
            MainAuthor = new(author)
        };
        await db.SaveAsync(book);

        var pipeline = new Template<BookEntity, AuthorEntity>(
                @"
                [
                    {
                        $match: { _id: <book_id> }
                    },
                    {
                        $lookup: 
                        {
                            from: '<author_collection>',
                            localField: '<MainAuthor.ID>',
                            foreignField: '_id',
                            as: 'authors'
                        }
                    },
                    {
                        $replaceWith: { $arrayElemAt: ['$authors', 0] }
                    },
                    {
                        $set: { <Surname> : '$<Name>' }
                    }
                ]").Tag("book_id", $"ObjectId('{book.ID}')")
                   .Tag("author_collection", db.CollectionName<AuthorEntity>())
                   .Path(b => b.MainAuthor.ID)
                   .PathOfResult(a => a.Surname)
                   .PathOfResult(a => a.Name);

        var result = (await (await db.PipelineCursorAsync(pipeline))
                          .ToListAsync())
            .Single();

        Assert.AreEqual(guid, result.Surname);
        Assert.AreEqual(guid, result.Name);
    }

    [TestMethod]
    public void throws_when_template_not_a_stage_array()
    {
        var pipeline = new Template<BookEntity>("{$match:{<Title>:'test'}}");

        Assert.ThrowsExactly<InvalidOperationException>(() => pipeline.AppendStage(""));
    }

    [TestMethod]
    public void throws_when_added_stage_not_json_object()
    {
        var pipeline = new Template<BookEntity>("[]");

        Assert.ThrowsExactly<ArgumentException>(() => pipeline.AppendStage("bleh"));
    }

    [TestMethod]
    public void appending_pipeline_stages()
    {
        var pipeline = new Template<BookEntity>("[{$match:{<Title>:'test'}}]");
        pipeline.AppendStage("{$match:{<Title>:'test'}}");
        pipeline.Property(b => b.Title);
        var res = pipeline.RenderToString();

        Assert.AreEqual("[{$match:{Title:'test'}},{$match:{Title:'test'}}]", res);
    }

    [TestMethod]
    public void appending_pipeline_stages_with_empty_pipeline()
    {
        var pipeline = new Template<BookEntity>("[]");
        pipeline.AppendStage("{$match:{<Title>:'test'}}");
        pipeline.AppendStage("{$match:{<Title>:'test'}}");
        pipeline.Property(b => b.Title);
        var res = pipeline.RenderToString();

        Assert.AreEqual("[{$match:{Title:'test'}},{$match:{Title:'test'}}]", res);
    }
}