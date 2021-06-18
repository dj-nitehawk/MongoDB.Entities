using Benchmark.Benchmarks;
using BenchmarkDotNet.Running;
using MongoDB.Entities;
using System.Threading.Tasks;

namespace Benchmark
{
    public static class Program
    {
        private static async Task Main()
        {
            await DB.InitAsync("benchmark-mongodb-entities");

            BenchmarkRunner.Run<CreateOne>();
        }
    }
}
