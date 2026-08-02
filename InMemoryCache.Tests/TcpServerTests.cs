using InMemoryCache.Client;
using InMemoryCache.Log;
using InMemoryCache.Server;
using InMemoryCache.Store;
using System.Net;

namespace InMemoryCache.Tests;

public class TcpServerTests
{
  [Fact]
  public async Task CorrectSetGetDeleteAsync()
  {
    static async Task SendFromClientAsync(string ip, int port, CancellationToken cancellationToken)
    {
      await SendSetAsync(ip, port, "user:1", "data", cancellationToken);
      await SendGetAsync(ip, port, "user:1", cancellationToken);
      await SendDeleteAsync(ip, port, "user:1", cancellationToken);
    }

    var lines = await SendFromClentToServerAndGetConsoleOutputAsLinesAsync(SendFromClientAsync);

    Assert.Contains("Server [127.0.0.1:8080]. Started.", lines[0]);
    Assert.Contains("Server [127.0.0.1:8080]. Client message min bytes for ArrayPool: 64.", lines[1]);

    Assert.Contains("Server [127.0.0.1:8080]. Client connected [127.0.0.1", lines[2]);

    Assert.Contains("Client [127.0.0.1", lines[3]);
    Assert.Contains("Command received [Command Type: SET, Key: user:1, Value: data].", lines[3]);
    Assert.Contains("Response sent [OK].", lines[3]);

    Assert.Contains("Client [127.0.0.1", lines[4]);
    Assert.Contains("Disconnected.", lines[4]);

    Assert.Contains("Server [127.0.0.1:8080]. Client connected [127.0.0.1", lines[5]);

    Assert.Contains("Client [127.0.0.1", lines[6]);
    Assert.Contains("Command received [Command Type: GET, Key: user:1].", lines[6]);
    Assert.Contains("Response sent [data].", lines[6]);

    Assert.Contains("Client [127.0.0.1", lines[7]);
    Assert.Contains("Disconnected.", lines[7]);

    Assert.Contains("Server [127.0.0.1:8080]. Client connected [127.0.0.1", lines[8]);

    Assert.Contains("Client [127.0.0.1", lines[9]);
    Assert.Contains("Command received [Command Type: DEL, Key: user:1].", lines[9]);
    Assert.Contains("Response sent [OK].", lines[9]);

    Assert.Contains("Client [127.0.0.1", lines[10]);
    Assert.Contains("Disconnected.", lines[10]);

    Assert.Contains("Server [127.0.0.1:8080]. Closed.", lines[11]);
  }

  [Fact]
  public async Task IncorrectSetAsync()
  {
    static async Task SendFromClientAsync(string ip, int port, CancellationToken cancellationToken)
    {
      await SendSetAsync(ip, port, "", "", cancellationToken);
    }

    var lines = await SendFromClentToServerAndGetConsoleOutputAsLinesAsync(SendFromClientAsync);

    Assert.Contains("Server [127.0.0.1:8080]. Started.", lines[0]);
    Assert.Contains("Server [127.0.0.1:8080]. Client message min bytes for ArrayPool: 64.", lines[1]);

    Assert.Contains("Server [127.0.0.1:8080]. Client connected [127.0.0.1", lines[2]);

    Assert.Contains("Client [127.0.0.1", lines[3]);
    Assert.Contains("Command received [Command Type: , Key: ].", lines[3]);
    Assert.Contains("Response sent [ERROR Unknown command].", lines[3]);

    Assert.Contains("Client [127.0.0.1", lines[4]);
    Assert.Contains("Disconnected.", lines[4]);

    Assert.Contains("Server [127.0.0.1:8080]. Closed.", lines[5]);
  }

  private static async Task<string[]> SendFromClentToServerAndGetConsoleOutputAsLinesAsync(
    Func<string, int, CancellationToken, Task> sendFromClientAsync
  )
  {
    using var stringWriterOutput = new StringWriter();
    var originalOutput = Console.Out;

    Console.SetOut(stringWriterOutput);

    try
    {
      await SendFromClientToServer(sendFromClientAsync);
    }
    finally
    {
      Console.SetOut(originalOutput);
    }

    var output = stringWriterOutput.ToString();
    var lines = output.Split(Environment.NewLine);

    return lines;
  }

  private static async Task SendFromClientToServer(Func<string, int, CancellationToken, Task> sendFromClientAsync)
  {
    var ip = "127.0.0.1";
    var ipAddress = IPAddress.Parse(ip);
    var port = 8080;
    var clientMessageMinBytes = 64;
    var logger = new ConsoleLogger();

    using var store = new SimpleStore();
    using var server = new TcpServer(ipAddress, port, clientMessageMinBytes, store, logger);

    using var cancellationTokenSource = new CancellationTokenSource();
    var cancellationToken = cancellationTokenSource.Token;

    var serverListeningTask = server.StartAsync(cancellationToken);

    await sendFromClientAsync(ip, port, cancellationToken);

    // Даем возможность серверу обработать данные клиента и записать лог в консоль
    await Task.Delay(1000);

    await cancellationTokenSource.CancelAsync();

    // Ждем перед завершением программы, чтобы сервер корректно обработал завершение работы
    await serverListeningTask;
  }

  private static async Task SendSetAsync(string ip, int port, string key, string value, CancellationToken cancellationToken)
  {
    using var client = new TcpClient();

    await client.ConnectAsync(ip, port, cancellationToken);

    var response = await client.SetAsync(key, value, cancellationToken);

    await client.DisconnectAsync(cancellationToken);
  }

  private static async Task SendGetAsync(string ip, int port, string key, CancellationToken cancellationToken)
  {
    using var client = new TcpClient();

    await client.ConnectAsync(ip, port, cancellationToken);

    var response = await client.GetAsync(key, cancellationToken);

    await client.DisconnectAsync(cancellationToken);
  }

  private static async Task SendDeleteAsync(string ip, int port, string key, CancellationToken cancellationToken)
  {
    using var client = new TcpClient();

    await client.ConnectAsync(ip, port, cancellationToken);

    var response = await client.DeleteAsync(key, cancellationToken);

    await client.DisconnectAsync(cancellationToken);
  }
}
