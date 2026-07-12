using System.Buffers;
using System.Net;
using System.Net.Sockets;
using InMemoryCache.Core;
using InMemoryCache.Parser;
using InMemoryCache.Store;

namespace InMemoryCache.Server;

// TODO: перенести serverSocket в поле класса и вынести ServerSocketInit
// TODO: использовать внутри TcpServer хранилище через интерфейс IStore
// TODO: сократить количество параметров конструктора TcpServer
// TODO: вынести Encoding.UTF8.GetString в Utils
// TODO: выделить ядро (разделить сервер на 3 класса: сервер, клиент и ядро). В ядро перенести ApplyCommandToStore
// TODO: создать отдельный класс для response, чтобы его можно было унаследовать от ILogWritable

public class TcpServer(IPAddress ipAddress, int port, int clientMessageMinBytes, SimpleStore store, ILogger logger) : IDisposable, ILogWritable
{
  private readonly IPEndPoint _endPoint = new(ipAddress, port);

  private readonly int _clientMessageMinBytes = clientMessageMinBytes;

  private readonly ILogger _logger = logger;

  private readonly SimpleStore _store = store;

  private static readonly byte[] OkResponse = CommandParser.GetBytes($"OK{Environment.NewLine}");

  private static readonly byte[] NullResponse = CommandParser.GetBytes($"NULL{Environment.NewLine}");

  private static readonly byte[] UnknownCommandResponse = CommandParser.GetBytes($"ERROR Unknown command{Environment.NewLine}");

  private bool disposed;

  public async Task StartAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

      serverSocket.Bind(_endPoint);
      serverSocket.Listen();

      _logger.WriteServerLog(this, "Started");
      _logger.WriteServerLog(this, $"Client message min bytes for ArrayPool: {_clientMessageMinBytes}");

      await WaitAndProcessClientsAsync(serverSocket, cancellationToken);
    }
    catch (Exception e)
    {
      _logger.WriteServerLog(this, e);
    }
  }

  private async Task WaitAndProcessClientsAsync(Socket serverSocket, CancellationToken cancellationToken = default)
  {
    try
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        var clientSocket = await serverSocket.AcceptAsync(cancellationToken);

        _logger.WriteServerLog(this, $"Client connected [{clientSocket.RemoteEndPoint}]");

        _ = Task.Run(() => ProcessClientAsync(clientSocket, cancellationToken), cancellationToken);
      }
    }
    catch (OperationCanceledException)
    {
      if (serverSocket.Connected) serverSocket.Shutdown(SocketShutdown.Both);
      serverSocket.Close();
      _logger.WriteServerLog(this, "Closed");
    }
  }

  private async Task ProcessClientAsync(Socket clientSocket, CancellationToken cancellationToken = default)
  {
    using (clientSocket)
    {
      try
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          var bytesReceived = await WaitAndProcessClientMessageAsync(clientSocket, cancellationToken);

          if (bytesReceived == 0) break;
        }
      }
      catch (Exception e) when (e is not OperationCanceledException)
      {
        _logger.WriteClientLog(clientSocket?.RemoteEndPoint?.ToString(), e);
      }
      finally
      {
        var clientEndPoint = clientSocket.RemoteEndPoint;

        if (clientSocket.Connected) clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
        _logger.WriteClientLog(clientEndPoint?.ToString(), "Disconnected");
      }
    }
  }

  private async Task<int> WaitAndProcessClientMessageAsync(Socket clientSocket, CancellationToken cancellationToken = default)
  {
    var buffer = ArrayPool<byte>.Shared.Rent(_clientMessageMinBytes);

    try
    {
      var bytesReceived = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

      if (bytesReceived != 0)
      {
        var response = ProcessClientMessage(buffer.AsMemory(0, bytesReceived), clientSocket.RemoteEndPoint);

        await clientSocket.SendAsync(response, SocketFlags.None, cancellationToken);
      }

      return bytesReceived;
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  private byte[] ProcessClientMessage(ReadOnlyMemory<byte> message, EndPoint? clientEndPoint)
  {
    var command = CommandParser.ParseBytes(message.Span);
    var response = ApplyCommandToStore(command);

    _logger.WriteClientLog(clientEndPoint, command, response);

    return response;
  }

  private byte[] ApplyCommandToStore(Command command)
  {
    if (string.IsNullOrEmpty(command.CommandType)) return UnknownCommandResponse;
    if (string.IsNullOrEmpty(command.Key)) return UnknownCommandResponse;
    if (command.CommandType == CommandParser.SetCommandType && command.Value.Length == 0) return UnknownCommandResponse;

    switch (command.CommandType)
    {
      case CommandParser.SetCommandType:
        _store.Set(command.Key, command.Value);

        return OkResponse;

      case CommandParser.GetCommandType:
        var value = _store.Get(command.Key);

        return value ?? NullResponse;

      case CommandParser.DeleteCommandType:
        _store.Delete(command.Key);

        return OkResponse;

      default:
        return UnknownCommandResponse;
    }
  }

  public string ToLogString() => _endPoint.ToString();

  protected virtual void Dispose(bool disposing)
  {
    if (!disposed)
    {
      if (disposing)
      {
        _store.Dispose();
      }

      disposed = true;
    }
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
