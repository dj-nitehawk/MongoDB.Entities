using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Entities;
using System;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class SavePartialVsUpdate : BenchBase
{
    private readonly Author author;

    public SavePartialVsUpdate()
    {
        author = new()
        {
            ID = ObjectId.GenerateNewId().ToString(),
            FirstName = "Test",
            LastName = "Test",
            Birthday = DateTime.UtcNow
        };
        author.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public Task Update()
    {
        return DB.Update<Author>()
            .MatchID(author.ID)
            .Modify(a => a.FirstName, "updated")
            .Modify(a => a.LastName, "updated")
            .ExecuteAsync();
    }

    [Benchmark]
    public Task SavePartial()
    {
        return author.SaveOnlyAsync(a => new
        {
            a.FirstName,
            a.LastName
        });
    }

    public override Task MongoDB_Entities()
    {
        throw new NotImplementedException();
    }

    public override Task Official_Driver()
    {
        throw new NotImplementedException();
    }
}

[MemoryDiagnoser]
public class DBContextVsStaticSave : BenchBase
{
    private readonly Author author;

    public DBContextVsStaticSave()
    {
        author = new()
        {
            FirstName = "Test",
            LastName = "Test",
            Birthday = DateTime.UtcNow
        };
    }

    [Benchmark]
    public Task DB_Context()
    {
        author.ID = null;
        return new DBContext().SaveAsync(author);
    }

    [Benchmark(Baseline = true)]
    public Task DB_Static()
    {
        author.ID = null;
        return DB.SaveAsync(author);
    }

    public override Task MongoDB_Entities() => throw new NotImplementedException();
    public override Task Official_Driver() => throw new NotImplementedException();
}
