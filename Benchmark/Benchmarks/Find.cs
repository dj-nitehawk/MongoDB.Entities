using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class FindOne : BenchBase
{
    readonly List<Author> list = new(1000);
    readonly string guid = Guid.NewGuid().ToString();

    public FindOne()
    {
        DB.Instance().Index<Author>()
                  .Key(a => a.FirstName!, KeyType.Ascending)
                  .Option(o => o.Background = false)
                  .CreateAsync()
                  .GetAwaiter()
                  .GetResult();

        for (var i = 1; i <= 1000; i++)
        {
            list.Add(new()
            {
                FirstName = i == 500 ? guid : "test",
            });
        }
        list.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.Instance()
                         .Find<Author>()
                         .Match(x => x.FirstName == guid)
                         .ExecuteAsync();
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
        await (await AuthorCollection.FindAsync(filter)).ToListAsync();
    }
}

[MemoryDiagnoser]
public class Find100 : BenchBase
{
    readonly List<Author> list = new(1000);
    readonly string guid = Guid.NewGuid().ToString();

    public Find100()
    {
        DB.Instance().Index<Author>()
                  .Key(a => a.FirstName!, KeyType.Ascending)
                  .Option(o => o.Background = false)
                  .CreateAsync()
                  .GetAwaiter()
                  .GetResult();

        for (var i = 1; i <= 1000; i++)
        {
            list.Add(new()
            {
                FirstName = i is > 500 and <= 600 ? guid : "test",
            });
        }
        list.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.Instance()
                         .Find<Author>()
                         .Match(x => x.FirstName == guid)
                         .ExecuteAsync();
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
        await (await AuthorCollection.FindAsync(filter)).ToListAsync();
    }
}

[MemoryDiagnoser]
public class FindFirst : BenchBase
{
    readonly List<Author> list = new(1000);
    readonly string guid = Guid.NewGuid().ToString();

    public FindFirst()
    {
        DB.Instance().Index<Author>()
                  .Key(a => a.FirstName!, KeyType.Ascending)
                  .Option(o => o.Background = false)
                  .CreateAsync()
                  .GetAwaiter()
                  .GetResult();

        for (var i = 1; i <= 1000; i++)
        {
            list.Add(new()
            {
                FirstName = i is > 500 and <= 600 ? guid : "test",
            });
        }
        list.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.Instance()
                         .Find<Author>()
                         .Match(x => x.FirstName == guid)
                         .ExecuteFirstAsync();
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
        await (await AuthorCollection.FindAsync(filter)).FirstOrDefaultAsync();
    }
}

[MemoryDiagnoser]
public class FindAny : BenchBase
{
    readonly List<Author> list = new(1000);
    readonly string guid = Guid.NewGuid().ToString();

    public FindAny()
    {
        DB.Instance().Index<Author>()
                  .Key(a => a.FirstName!, KeyType.Ascending)
                  .Option(o => o.Background = false)
                  .CreateAsync()
                  .GetAwaiter()
                  .GetResult();

        for (var i = 1; i <= 1000; i++)
        {
            list.Add(new()
            {
                FirstName = i is > 500 and <= 600 ? guid : "test",
            });
        }
        list.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.Instance()
                         .Find<Author>()
                         .Match(x => x.FirstName == guid)
                         .ExecuteAnyAsync();
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
        await (await AuthorCollection.FindAsync(filter)).AnyAsync();
    }
}

[MemoryDiagnoser]
public class FindSingle : BenchBase
{
    readonly List<Author> list = new(1000);
    readonly string guid = Guid.NewGuid().ToString();

    public FindSingle()
    {
        DB.Instance().Index<Author>()
                  .Key(a => a.FirstName!, KeyType.Ascending)
                  .Option(o => o.Background = false)
                  .CreateAsync()
                  .GetAwaiter()
                  .GetResult();

        for (var i = 1; i <= 1000; i++)
        {
            list.Add(new()
            {
                FirstName = i == 500 ? guid : "test",
            });
        }
        list.SaveAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        return DB.Instance()
                         .Find<Author>()
                         .Match(x => x.FirstName == guid)
                         .ExecuteSingleAsync();
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
        await (await AuthorCollection.FindAsync(filter)).SingleOrDefaultAsync();
    }
}
