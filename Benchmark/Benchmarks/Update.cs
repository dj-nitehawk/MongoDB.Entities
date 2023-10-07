using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class UpdateOne : BenchBase
{
    readonly string id = ObjectId.GenerateNewId().ToString();

    public UpdateOne()
    {
        DB.SaveAsync(new Author { ID = id, FirstName = "initial" }).GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.Update<Author>()
                 .MatchID(id)
                 .Modify(a => a.FirstName, "updated")
                 .ExecuteAsync();
    }

    [Benchmark(Baseline = true)]
    public override Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.ID == id);
        var update = Builders<Author>.Update.Set(a => a.FirstName, "updated");
        return AuthorCollection.UpdateOneAsync(filter, update);
    }
}

[MemoryDiagnoser]
public class Update100 : BenchBase
{
    readonly List<Author> list = new(1000);
    readonly string guid = Guid.NewGuid().ToString();

    public Update100()
    {
        DB.Index<Author>()
          .Key(a => a.FirstName!, KeyType.Ascending)
          .Option(o => o.Background = false)
          .CreateAsync()
          .GetAwaiter()
          .GetResult();

        for (int i = 1; i <= 1000; i++)
        {
            list.Add(new Author
            {
                FirstName = i is > 500 and <= 600 ? guid : "test",
            });
        }
        list.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB
            .Update<Author>()
            .Match(x => x.FirstName == guid)
            .Modify(x => x.FirstName, "updated")
            .ExecuteAsync();
    }

    [Benchmark(Baseline = true)]
    public override Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
        var update = Builders<Author>.Update.Set(a => a.FirstName, "updated");
        return AuthorCollection.UpdateManyAsync(filter, update);
    }
}
