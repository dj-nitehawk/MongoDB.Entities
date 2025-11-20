using MongoDB.Entities;

namespace Benchmark;

public class File : FileEntity<File>
{
    public string Name { get; set; } = null!;
}
