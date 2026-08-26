using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InMemoryCache.Telemetry;

public static class TelemetryWrapper
{
  private static readonly string _serviceName = "in-memory-cache";
  private static readonly string _libName = "InMemoryCache";

  public static TracerProvider? TracerProvider { private get; set; }

  public static MeterProvider? MeterProvider { private get; set; }

  public static readonly ActivitySource ActivitySource = new(_libName);

  private static readonly Meter Meter = new(_libName);

  public static readonly Counter<long> CommandCounter = Meter.CreateCounter<long>(nameof(CommandCounter));

  public static readonly Histogram<long> CommandHistogram = Meter.CreateHistogram<long>(
    name: nameof(CommandHistogram),
    unit: "ms"
  );

  public static TracerProvider BuildConsoleTracerProvider() => Sdk
    .CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(_serviceName))
    .AddSource(_libName)
    .AddConsoleExporter()
    .Build();

  public static MeterProvider BuildConsoleMeterProvider() => Sdk
    .CreateMeterProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(_serviceName))
    .AddMeter(_libName)
    .AddConsoleExporter()
    .Build();

  public static void Dispose()
  {
    ActivitySource.Dispose();
    TracerProvider?.Dispose();

    Meter.Dispose();
    MeterProvider?.Dispose();
  }
}
