using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using System.Threading.Tasks;

namespace Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    public class UpdateOne : BenchBase
    {
        private readonly string id = ObjectId.GenerateNewId().ToString();

        public UpdateOne()
        {
            Initialize();
            DB.SaveAsync(new Author { ID = id, FirstName = "initial" }).GetAwaiter().GetResult();
        }

        [Benchmark]
        public override Task MongoDB_Entities()
        {
            return DB.Update<Author>()
                     .MatchID(id)
                     .Modify(a => a.FirstName, "updated")
                     .ExecuteAsync();
        }

        [Benchmark(Baseline = true)]
        public override Task Official_Driver()
        {
            var filter = Builders<Author>.Filter.Where(a => a.ID == id);
            var update = Builders<Author>.Update.Set(a => a.FirstName, "updated");
            return AuthorCollection.UpdateOneAsync(filter, update);
        }
    }
}
