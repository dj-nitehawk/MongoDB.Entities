using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Templates
    {
        [TestMethod]
        public void missing_tags_throws()
        {
            var template = new Template(@"[
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

            Assert.ThrowsException<InvalidOperationException>(() => template.ToString());
        }

        [TestMethod]
        public void extra_tags_throws()
        {
            var template = new Template(@"[
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

            Assert.ThrowsException<InvalidOperationException>(() => template.ToString());
        }

        [TestMethod]
        public void tag_replacement_works()
        {
            var template = new Template(@"
            {
               $match: { '<OtherAuthors.Name>': /<search_term>/is }
            }")

            .Path<Book>(b => b.OtherAuthors[0].Name)
            .Tag("search_term", "Eckhart Tolle");

            const string expectation = @"
            {
               $match: { 'OtherAuthors.Name': /Eckhart Tolle/is }
            }";

            Assert.AreEqual(expectation, template.ToString());
        }

        [TestMethod]
        public async Task tag_replacement_with_db_aggregate()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid, Age = 54 };
            var author2 = new Author { Name = guid, Age = 53 };
            await DB.SaveAsync(new[] { author1, author2 });

            var pipeline = new Template<Author>(@"
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

            var results = await (await DB.AggregateAsync(pipeline)).ToListAsync();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[0].Name == guid);
            Assert.IsTrue(results.Last().Age == 54);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task aggregation_pipeline_with_differnt_input_and_output_typesAsync()
        {
            var guid = Guid.NewGuid().ToString();

            var author = new Author { Name = guid };
            await author.SaveAsync();

            var book = new Book { Title = guid, MainAuthor = author };
            await book.SaveAsync();

            var pipeline = new Template<Book, Author>(@"
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
                ]"
            ).Tag("book_id", $"ObjectId('{book.ID}')")
             .Tag("author_collection", DB.Entity<Author>().CollectionName())
             .Path(b => b.MainAuthor.ID)
             .PathOfResult(a => a.Surname)
             .PathOfResult(a => a.Name);

            var result = (await (await DB.AggregateAsync(pipeline))
                           .ToListAsync())
                           .Single();

            Assert.AreEqual(guid, result.Surname);
            Assert.AreEqual(guid, result.Name);
        }
    }
}
