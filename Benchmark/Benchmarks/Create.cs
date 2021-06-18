using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Entities;
using System;
using System.Threading.Tasks;

namespace Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    public class CreateOne : BenchBase
    {
        public CreateOne()
        {
            Initialize();
        }

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
}
