using System.Net;
using InMemoryCache.Log;
using InMemoryCache.Server;
using InMemoryCache.Store;

var ipAddress = IPAddress.Parse("127.0.0.1");
var port = 8180;
var clientMessageMinBytes = 128;
var logger = new NumbLogger();

using var store = new SimpleStore();
using var server = new TcpServer(ipAddress, port, clientMessageMinBytes, store, logger);

using var cancellationTokenSource = new CancellationTokenSource();

var serverListeningTask = server.StartAsync(cancellationTokenSource.Token);

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

await cancellationTokenSource.CancelAsync();
await serverListeningTask;
