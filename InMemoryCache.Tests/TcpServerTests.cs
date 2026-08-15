using InMemoryCache.Client;
using InMemoryCache.Core;
using InMemoryCache.Log;
using InMemoryCache.Server;
using InMemoryCache.Store;
using System.Net;

namespace InMemoryCache.Tests;

public class TcpServerTests
{
  [Fact]
  public async Task CorrectSetGetDeleteAsync_DifferentConnections()
  {
    static async Task SendFromClientAsync(string ip, int port, CancellationToken cancellationToken)
    {
      var profile = new UserProfile()
      {
        Id = 1000,
        Username = "John Smith",
        CreatedAt = new DateTime(2026, 8, 1),
      };

      await TcpClient.SetAsync(ip, port, "user:1", profile, cancellationToken: cancellationToken);
      await TcpClient.GetAsync(ip, port, "user:1", cancellationToken: cancellationToken);
      await TcpClient.DeleteAsync(ip, port, "user:1", cancellationToken: cancellationToken);
    }

    var lines = await SendFromClentToServerAndGetConsoleOutputAsLinesAsync(SendFromClientAsync);

    Assert.Contains("Server [127.0.0.1:8180]. Started.", lines[0]);
    Assert.Contains("Server [127.0.0.1:8180]. Client message min bytes for ArrayPool: 128.", lines[1]);

    Assert.Contains("Server [127.0.0.1:8180]. Client connected [127.0.0.1", lines[2]);

    Assert.Contains("Client [127.0.0.1", lines[3]);
    Assert.Contains("Command received [Command Type: SET, Key: user:1, Value: {\"Id\":1000,\"Username\":\"John Smith\",\"CreatedAt\":\"2026-08-01T00:00:00\"}].", lines[3]);
    Assert.Contains("Response sent [OK].", lines[3]);

    Assert.Contains("Client [127.0.0.1", lines[4]);
    Assert.Contains("Disconnected.", lines[4]);

    Assert.Contains("Server [127.0.0.1:8180]. Client connected [127.0.0.1", lines[5]);

    Assert.Contains("Client [127.0.0.1", lines[6]);
    Assert.Contains("Command received [Command Type: GET, Key: user:1].", lines[6]);
    Assert.Contains("Response sent [{\"Id\":1000,\"Username\":\"John Smith\",\"CreatedAt\":\"2026-08-01T00:00:00\"}].", lines[6]);

    Assert.Contains("Client [127.0.0.1", lines[7]);
    Assert.Contains("Disconnected.", lines[7]);

    Assert.Contains("Server [127.0.0.1:8180]. Client connected [127.0.0.1", lines[8]);

    Assert.Contains("Client [127.0.0.1", lines[9]);
    Assert.Contains("Command received [Command Type: DEL, Key: user:1].", lines[9]);
    Assert.Contains("Response sent [OK].", lines[9]);

    Assert.Contains("Client [127.0.0.1", lines[10]);
    Assert.Contains("Disconnected.", lines[10]);

    Assert.Contains("Server [127.0.0.1:8180]. Closed.", lines[11]);
  }

  [Fact]
  public async Task CorrectSetGetDeleteAsync_OneConnection()
  {
    static async Task SendFromClientAsync(string ip, int port, CancellationToken cancellationToken)
    {
      var profile = new UserProfile()
      {
        Id = 1000,
        Username = "John Smith",
        CreatedAt = new DateTime(2026, 8, 1),
      };

      await using var client = await TcpClient.CreateAsync(ip, port, cancellationToken: cancellationToken);
      await client.SetAsync("user:1", profile, cancellationToken);
      await client.GetAsync("user:1", cancellationToken);
      await client.DeleteAsync("user:1", cancellationToken);
    }

    var lines = await SendFromClentToServerAndGetConsoleOutputAsLinesAsync(SendFromClientAsync);

    Assert.Contains("Server [127.0.0.1:8080]. Started.", lines[0]);
    Assert.Contains("Server [127.0.0.1:8080]. Client message min bytes for ArrayPool: 128.", lines[1]);

    Assert.Contains("Server [127.0.0.1:8080]. Client connected [127.0.0.1", lines[2]);

    Assert.Contains("Client [127.0.0.1", lines[3]);
    Assert.Contains("Command received [Command Type: SET, Key: user:1, Value: {\"Id\":1000,\"Username\":\"John Smith\",\"CreatedAt\":\"2026-08-01T00:00:00\"}].", lines[3]);
    Assert.Contains("Response sent [OK].", lines[3]);

    Assert.Contains("Client [127.0.0.1", lines[4]);
    Assert.Contains("Command received [Command Type: GET, Key: user:1].", lines[4]);
    Assert.Contains("Response sent [{\"Id\":1000,\"Username\":\"John Smith\",\"CreatedAt\":\"2026-08-01T00:00:00\"}].", lines[4]);

    Assert.Contains("Client [127.0.0.1", lines[5]);
    Assert.Contains("Command received [Command Type: DEL, Key: user:1].", lines[5]);
    Assert.Contains("Response sent [OK].", lines[5]);

    Assert.Contains("Client [127.0.0.1", lines[6]);
    Assert.Contains("Disconnected.", lines[6]);

    Assert.Contains("Server [127.0.0.1:8080]. Closed.", lines[7]);
  }

  [Fact]
  public async Task IncorrectSetAsync()
  {
    static async Task SendFromClientAsync(string ip, int port, CancellationToken cancellationToken)
    {
      await TcpClient.SetAsync(ip, port, "", new UserProfile(), cancellationToken: cancellationToken);
    }

    var lines = await SendFromClentToServerAndGetConsoleOutputAsLinesAsync(SendFromClientAsync);

    Assert.Contains("Server [127.0.0.1:8180]. Started.", lines[0]);
    Assert.Contains("Server [127.0.0.1:8180]. Client message min bytes for ArrayPool: 128.", lines[1]);

    Assert.Contains("Server [127.0.0.1:8180]. Client connected [127.0.0.1", lines[2]);

    Assert.Contains("Client [127.0.0.1", lines[3]);
    Assert.Contains("Command received [Command Type: , Key: ].", lines[3]);
    Assert.Contains("Response sent [ERROR Unknown command].", lines[3]);

    Assert.Contains("Client [127.0.0.1", lines[4]);
    Assert.Contains("Disconnected.", lines[4]);

    Assert.Contains("Server [127.0.0.1:8180]. Closed.", lines[5]);
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
    var port = 8180;
    var clientMessageMinBytes = 128;
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
}
