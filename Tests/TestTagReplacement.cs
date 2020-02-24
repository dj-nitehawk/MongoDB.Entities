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
    }
}
