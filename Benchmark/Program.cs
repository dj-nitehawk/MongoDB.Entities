using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main()
        {
            //BenchmarkRunner.Run(typeof(Program).Assembly);

            BenchmarkRunner.Run<FileStorage>();

            //new FileStorage().MongoDB_Entities().GetAwaiter().GetResult();
            //Console.ReadLine();
        }
    }
}
