using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class CreateOne : BenchBase
{
    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.SaveAsync(new Author
        {
            FirstName = "test",
            LastName = "test",
            Birthday = DateTime.UtcNow,
        });
    }

    [Benchmark(Baseline = true)]
    public override Task Official_Driver()
    {
        return AuthorCollection.InsertOneAsync(new Author
        {
            ID = ObjectId.GenerateNewId().ToString(),
            FirstName = "test",
            LastName = "test",
            Birthday = DateTime.UtcNow,
        });
    }
}

[MemoryDiagnoser]
public class CreateBulk : BenchBase
{
    private readonly List<Author> list = new(1000);

    public CreateBulk()
    {
        for (int i = 1; i <= 1000; i++)
        {
            list.Add(new Author
            {
                FirstName = "test",
                LastName = "test",
                Birthday = DateTime.UtcNow
            });
        }
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        foreach (var author in list) author.ID = null!;
        return DB.SaveAsync(list);
    }

    [Benchmark(Baseline = true)]
    public override Task Official_Driver()
    {
        var models = new List<WriteModel<Author>>(list.Count);
        foreach (var author in list)
        {
            author.ID = (string)author.GenerateNewID();
            models.Add(new InsertOneModel<Author>(author));
        }
        return AuthorCollection.BulkWriteAsync(models);
    }
}
