using BenchmarkDotNet.Running;

namespace Benchmark;

public static class Program
{
    static void Main()
    {
        BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}

//dotnet run -p Benchmark.csproj -c Release
