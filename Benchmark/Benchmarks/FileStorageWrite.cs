using BenchmarkDotNet.Attributes;
using MongoDB.Driver.GridFS;
using MongoDB.Entities;
using System.IO;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public class FileStorageWrite : BenchBase
{
    static readonly MemoryStream _memStream = new(new byte[32 * 1024 * 1024]);

    [Benchmark]
    public override async Task MongoDB_Entities()
    {
        var db = DB.Default;

        _memStream.Position = 0;

        var file = new File { Name = "file name here" };
        await db.SaveAsync(file);
        await file.Data(db).UploadAsync(_memStream, 1024 * 4);
    }

    [Benchmark(Baseline = true)]
    public override async Task Official_Driver()
    {
        _memStream.Position = 0;

        var bucket = new GridFSBucket(
            Database,
            new()
            {
                BucketName = "benchmark",
                ChunkSizeBytes = 4 * 1024 * 1024
            });

        await bucket.UploadFromStreamAsync("file name here", _memStream);
    }
}