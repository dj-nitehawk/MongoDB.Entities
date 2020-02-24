using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TagReplacement
    {
        [TestMethod]
        public void missing_tags_throws()
        {
            var command = @"
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
            }";

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                _ = command.Replace(
                    ("user", "$user"),
                    ("count", "$sum"),
                    ("max.distance", "max.distance.value"));
            });
        }

        [TestMethod]
        public void extra_tags_throws()
        {
            var command = @"
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
            }";

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                _ = command.Replace(
                    ("user", "$user"),
                    ("size", "$size"));
            });
        }

        [TestMethod]
        public void tag_replacement_works()
        {
            const string template = @"
            {
                $match: {
                    '<OtherAuthors.Name>': /<search_term>/is
                }
            }";

            var result = template.Replace(
                         ("OtherAuthors.Name", Prop.Dotted<Book>(b => b.OtherAuthors[0].Name)),
                         ("search_term", "Eckhart Tolle"));

            const string expectation = @"
            {
                $match: {
                    'OtherAuthors.Name': /Eckhart Tolle/is
                }
            }";

            Assert.AreEqual(expectation, result);
        }
    }
}
