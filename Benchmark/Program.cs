using BenchmarkDotNet.Running;

namespace Benchmark
{
    public static class Program
    {
        private static void Main()
        {
            //BenchmarkRunner.Run(typeof(Program).Assembly);
            BenchmarkRunner.Run<Relationships>();
        }
    }
}
