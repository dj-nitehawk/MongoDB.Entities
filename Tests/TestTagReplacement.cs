using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;

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

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                template.ToString();
            });
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

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                template.ToString();
            });
        }

        [TestMethod]
        public void tag_replacement_works()
        {
            var template = new Template(@"
            {
               $match: { '<OtherAuthors.Name>': /<search_term>/is }
            }")

            .Dotted<Book>(b => b.OtherAuthors[0].Name)
            .Tag("search_term", "Eckhart Tolle");

            const string expectation = @"
            {
               $match: { 'OtherAuthors.Name': /Eckhart Tolle/is }
            }";

            Assert.AreEqual(expectation, template.ToString());
        }

        [TestMethod]
        public void tag_replacement_with_db_aggregate()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid, Age = 54 };
            var author2 = new Author { Name = guid, Age = 53 };
            DB.Save(new[] { author1, author2 });

            var pipeline = new Template<Author>(@"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                .Dotted(a => a.Name)
                .Tag("author_name", guid)
                .Dotted(a => a.Age);

            var results = DB.Aggregate(pipeline).ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.First().Name == guid);
            Assert.IsTrue(results.Last().Age == 54);
        }
    }
}
