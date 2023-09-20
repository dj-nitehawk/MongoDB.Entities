using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.Entities;
using System.IO;
using System.Threading.Tasks;

namespace Benchmark;

[MemoryDiagnoser]
public partial class FileStorageRead : BenchBase
{
    private static readonly MemoryStream memStream = new(new byte[32 * 1024 * 1024]);

    private readonly string? fEntityID;
    private readonly ObjectId gridFSId;

    public FileStorageRead()
    {
        fEntityID = WriteFileME().GetAwaiter().GetResult();
        gridFSId = WriteFileGridFS().GetAwaiter().GetResult();
    }

    public async Task<string> WriteFileME()
    {
        memStream.Position = 0;
        var file = new File { Name = "file name here" };
        await file.SaveAsync();
        await file.Data.UploadAsync(memStream, 1024 * 4);
        return file.ID;
    }

    public Task<ObjectId> WriteFileGridFS()
    {
        memStream.Position = 0;
        var bucket = new GridFSBucket(Database, new GridFSBucketOptions
        {
            BucketName = "benchmark",
            ChunkSizeBytes = 4 * 1024 * 1024
        });
        return bucket.UploadFromStreamAsync("file name here", memStream);
    }

    [Benchmark]
    public override Task MongoDB_Entities()
    {
        memStream.Position = 0;
        return DB.File<File>(fEntityID).DownloadAsync(memStream);
    }

    [Benchmark(Baseline = true)]
    public override Task Official_Driver()
    {
        memStream.Position = 0;
        var bucket = new GridFSBucket(Database, new GridFSBucketOptions
        {
            BucketName = "benchmark",
            ChunkSizeBytes = 4 * 1024 * 1024
        });
        return bucket.DownloadToStreamAsync(gridFSId, memStream);
    }
}
