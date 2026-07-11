using System.Net;
using Store;
using Store.Store;

var ipAddress = IPAddress.Parse("127.0.0.1");
var port = 8080;
var clientMessageMinBytes = 64;
using var store = new SimpleStore();
using var server = new TcpServer(ipAddress, port, clientMessageMinBytes, store);

using var cancellationTokenSource = new CancellationTokenSource();

var serverListeningTask = server.StartAsync(cancellationTokenSource.Token);

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

await cancellationTokenSource.CancelAsync();
await serverListeningTask;
