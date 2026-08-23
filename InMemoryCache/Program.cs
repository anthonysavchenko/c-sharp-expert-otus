using System.Net;
using InMemoryCache.Log;
using InMemoryCache.Server;
using InMemoryCache.Store;
using InMemoryCache.Telemetry;

// TelemetryWrapper.TracerProvider = TelemetryWrapper.BuildConsoleTracerProvider();
// TelemetryWrapper.MeterProvider = TelemetryWrapper.BuildConsoleMeterProvider();

var ipAddress = IPAddress.Parse("127.0.0.1");
var port = 8180;
var messageMaxLengthBytes = 128;
var maxConcurrentClients = 10;
var logger = new NumbLogger();

using var store = new SimpleStore();
using var server = new TcpServer(ipAddress, port, messageMaxLengthBytes, maxConcurrentClients, store, logger);

using var cancellationTokenSource = new CancellationTokenSource();

var serverListeningTask = server.StartAsync(cancellationTokenSource.Token);

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

await cancellationTokenSource.CancelAsync();
await serverListeningTask;
