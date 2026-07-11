using System.Net;
using System.Net.Sockets;
using Store.Parser;
using Store.Store;

namespace Store.Tests;

public class TcpServerTests
{
  [Fact]
  public async Task CorrectSet()
  {
    var lines = await SendFromClentToServerAndGetConsoleOutputAsLines(["SET user:1 data", "GET user:1", "DEL user:1"]);

    Assert.Contains("Server 127.0.0.1:8080. Started", lines[0]);
    Assert.Contains("Client message min bytes for ArrayPool: 64", lines[1]);

    Assert.Contains("Client 127.0.0.1", lines[2]);
    Assert.Contains("Connected", lines[2]);

    Assert.Contains("Client 127.0.0.1", lines[3]);
    Assert.Contains("Received command type: SET, key: user:1, value: data. Response sent: OK.", lines[3]);

    Assert.Contains("Client 127.0.0.1", lines[4]);
    Assert.Contains("Disconnected", lines[4]);

    Assert.Contains("Client 127.0.0.1", lines[5]);
    Assert.Contains("Connected", lines[5]);

    Assert.Contains("Client 127.0.0.1", lines[6]);
    Assert.Contains("Received command type: GET, key: user:1. Response sent: data.", lines[6]);

    Assert.Contains("Client 127.0.0.1", lines[7]);
    Assert.Contains("Disconnected", lines[7]);

    Assert.Contains("Client 127.0.0.1", lines[8]);
    Assert.Contains("Connected", lines[8]);

    Assert.Contains("Client 127.0.0.1", lines[9]);
    Assert.Contains("Received command type: DEL, key: user:1. Response sent: OK.", lines[9]);

    Assert.Contains("Client 127.0.0.1", lines[10]);
    Assert.Contains("Disconnected", lines[10]);

    Assert.Contains("Server 127.0.0.1:8080. Closed", lines[11]);
  }

  [Fact]
  public async Task IncorrectSet()
  {
    var lines = await SendFromClentToServerAndGetConsoleOutputAsLines(["SET"]);

    Assert.Contains("Server 127.0.0.1:8080. Started", lines[0]);
    Assert.Contains("Client message min bytes for ArrayPool: 64", lines[1]);

    Assert.Contains("Client 127.0.0.1", lines[2]);
    Assert.Contains("Connected", lines[2]);

    Assert.Contains("Client 127.0.0.1", lines[3]);
    Assert.Contains("Received command type: , key: . Response sent: ERR Unknown command.", lines[3]);

    Assert.Contains("Client 127.0.0.1", lines[4]);
    Assert.Contains("Disconnected", lines[4]);

    Assert.Contains("Server 127.0.0.1:8080. Closed", lines[5]);
  }

  private static async Task<string[]> SendFromClentToServerAndGetConsoleOutputAsLines(string[] messages)
  {
    using var stringWriterOutput = new StringWriter();
    var originalOutput = Console.Out;

    Console.SetOut(stringWriterOutput);

    try
    {
      await SendFromClientToServer(messages);
    }
    finally
    {
      Console.SetOut(originalOutput);
    }

    var output = stringWriterOutput.ToString();
    var lines = output.Split(Environment.NewLine);

    return lines;
  }

  private static async Task SendFromClientToServer(string[] messages)
  {
    var ipAddress = IPAddress.Parse("127.0.0.1");
    var port = 8080;
    var clientMessageMinBytes = 64;

    using var store = new SimpleStore();
    using var server = new TcpServer(ipAddress, port, clientMessageMinBytes, store);

    using var cancellationTokenSource = new CancellationTokenSource();
    var cancellationToken = cancellationTokenSource.Token;

    var serverListeningTask = server.StartAsync(cancellationToken);

    var serverEndPoint = new IPEndPoint(ipAddress, port);

    foreach (var message in messages)
    {
      await SendFromClient(message, serverEndPoint, cancellationToken);

      // Даем возможность серверу обработать данные клиента и записать лог в консоль
      await Task.Delay(1000);
    }

    await cancellationTokenSource.CancelAsync();

    // Ждем перед завершением программы, чтобы сервер корректно обработал завершение работы
    await serverListeningTask;
  }

  private static async Task SendFromClient(string message, EndPoint serverEndPoint, CancellationToken cancellationToken)
  {
    using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

    await client.ConnectAsync(serverEndPoint, cancellationToken);
    await client.SendAsync(CommandParser.GetBytes(message), SocketFlags.None, cancellationToken);

    var response = new byte[64];

    await client.ReceiveAsync(response, SocketFlags.None, cancellationToken);
    await client.DisconnectAsync(reuseSocket: false, cancellationToken);

    client.Shutdown(SocketShutdown.Both);
    client.Close();
  }
}
