using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Entities;
using System;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class SavePartialVsUpdate : BenchBase
{
    readonly Author author;
    readonly DB db = DB.Default;

    public SavePartialVsUpdate()
    {
        author = new()
        {
            ID = ObjectId.GenerateNewId().ToString()!,
            FirstName = "Test",
            LastName = "Test",
            Birthday = DateTime.UtcNow
        };
        db.SaveAsync(author).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public Task Update()
    {
        return db.Update<Author>()
                 .MatchID(author.ID)
                 .Modify(a => a.FirstName, "updated")
                 .Modify(a => a.LastName, "updated")
                 .ExecuteAsync();
    }

    [Benchmark]
    public Task SavePartial()
    {
        return db.SaveOnlyAsync(
            author,
            a => new
            {
                a.FirstName,
                a.LastName
            });
    }

    public override Task MongoDB_Entities()
        => throw new NotImplementedException();

    public override Task Official_Driver()
        => throw new NotImplementedException();
}