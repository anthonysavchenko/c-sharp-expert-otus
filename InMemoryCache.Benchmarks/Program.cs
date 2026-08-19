using BenchmarkDotNet.Running;

namespace InMemoryCache.Benchmarks;

internal class Program
{
  private static void Main(string[] _)
  {
    BenchmarkRunner.Run<SerializationBenchmark>();
  }
}
