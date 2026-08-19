using System.Text.Json;
using BenchmarkDotNet.Attributes;
using InMemoryCache.Core;

namespace InMemoryCache.Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmark
{
  private readonly UserProfile _userProfile;

  public SerializationBenchmark()
  {
    _userProfile = new UserProfile
    {
      Id = 1000,
      Username = "John Smith",
      CreatedAt = new DateTime(2026, 8, 1),
    };
  }

  private readonly JsonSerializerOptions _stjOptions = new(JsonSerializerDefaults.General);

  [Benchmark(Baseline = true)]
  public byte[] SerializeWithJsonSerializer()
  {
    return JsonSerializer.SerializeToUtf8Bytes(_userProfile, _stjOptions);
  }

  [Benchmark]
  public byte[] SerializeWithSourceGenerator()
  {
    using var memoryStream = new MemoryStream();

    _userProfile.SerializeToBinary(memoryStream);

    return memoryStream.ToArray();
  }
}
