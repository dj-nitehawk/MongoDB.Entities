using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class Watcher : BenchBase
{
    [Benchmark]
    public override async Task MongoDB_Entities()
    {
        var cts = new CancellationTokenSource();

        var watcher = DBInstance.Instance().Watcher<Book>(Guid.NewGuid().ToString());

        watcher.OnChangesCSD += csDocs =>
        {
            foreach (var _ in csDocs)
            {
                //Console.WriteLine(csd.FullDocument.Title);
                cts.Cancel();
            }
        };

        watcher.Start(EventType.Created, cancellation: cts.Token);

        await InsertAnEntity();

        while (!cts.IsCancellationRequested)
        {
            await Task.Delay(1);
        }

        cts.Dispose();
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        var cts = new CancellationTokenSource();
        var filter = Builders<ChangeStreamDocument<Book>>.Filter.Where(x => x.OperationType == ChangeStreamOperationType.Insert);
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Book>>().Match(filter);

        var cursor = await BookCollection.WatchAsync(pipeline, cancellationToken: cts.Token);

        _ = cursor.ForEachAsync(_ =>
         {
             //Console.WriteLine(csd.FullDocument.Title);
             cts.Cancel();
         });

        await InsertAnEntity();

        while (!cts.IsCancellationRequested)
        {
            await Task.Delay(1);
        }

        cts.Dispose();
    }

    Task InsertAnEntity()
        => new Book { Title = "book name" }.SaveAsync();
}