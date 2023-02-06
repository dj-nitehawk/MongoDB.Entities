using MongoDB.Entities;

namespace Benchmark;

public class File : FileEntity
{
    public string Name { get; set; } = null!;
}
