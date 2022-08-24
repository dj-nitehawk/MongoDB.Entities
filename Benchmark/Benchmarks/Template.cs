using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class Template : BenchBase
{
    [Benchmark]
    public override Task MongoDB_Entities()
    {
        var pipeline = new Template<Book, Author>(@"
            [
                {
                    $match: { _id: ObjectId('<book_id>') }
                },
                {
                    $lookup: 
                    {
                        from: '<Author>',
                        localField: '<Author.ID>',
                        foreignField: '_id',
                        as: 'authors'
                    }
                },
                {
                    $replaceWith: { $arrayElemAt: ['$authors', 0] }
                },
                {
                    $set: { <Age> : '<age_value>' }
                }
            ]")
          .Tag("book_id", "5e572df44467000021005692")
          .Tag("age_value", "34")
          .Collection<Author>()
          .Path(b => b.Author.ID)
          .PathOfResult(a => a.Age);

        return DB.PipelineAsync(pipeline);
    }

    [Benchmark]
    public Task MongoDB_Entities_No_Cache()
    {
        var pipeline = new Template<Book, Author>(@"
            [
                {
                    $match: { _id: ObjectId('<book_id>') }
                },
                {
                    $lookup: 
                    {
                        from: '<Author>',
                        localField: '<Author.ID>',
                        foreignField: '_id',
                        as: 'authors'
                    }
                },
                {
                    $replaceWith: { $arrayElemAt: ['$authors', 0] }
                },
                {
                    $set: { <Age> : '<age_value>' }
                }
            ]");

        pipeline.AppendStage("{$sort:{_id:1}}"); //this disables caching

        pipeline
            .Tag("book_id", "5e572df44467000021005692")
            .Tag("age_value", "34")
            .Collection<Author>()
            .Path(b => b.Author.ID)
            .PathOfResult(a => a.Age);

        return DB.PipelineAsync(pipeline);
    }

    [Benchmark(Baseline = true)]
    public override Task Official_Driver()
    {
        var pipeline = BookCollection.Aggregate()
            .Match(b => b.ID == "5e572df44467000021005692")
            .Lookup<Book, Author, BsonDocument>(
                AuthorCollection,
                b => b.Author.ID,
                a => a.ID,
                x => x["authors"])
            .AppendStage<Author>("{$replaceWith: { $arrayElemAt: ['$authors', 0] }}")
            .AppendStage<Author>("{$set: { Age : '34' }}");

        return pipeline.ToListAsync();
    }
}
