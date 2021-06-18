using Benchmark.Benchmarks;
using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace Benchmark
{
    public static class Program
    {
        private static async Task Main()
        {
            //BenchmarkRunner.Run<CreateOne>();
            //BenchmarkRunner.Run<CreateBulk>();
            //BenchmarkRunner.Run<UpdateOne>();
            //BenchmarkRunner.Run<FindOne>();
            BenchmarkRunner.Run<Find100>();
        }
    }
}
