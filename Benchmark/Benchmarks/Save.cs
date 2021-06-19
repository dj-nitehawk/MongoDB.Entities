using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using MongoDB.Entities;
using BenchmarkDotNet.Attributes;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class SavePartialVsUpdate : BenchBase
    {
        private readonly Author author;

        public SavePartialVsUpdate()
        {
            Initialize();
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
}
