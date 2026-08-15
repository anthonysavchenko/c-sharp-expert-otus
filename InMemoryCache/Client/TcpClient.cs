using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using InMemoryCache.Core;
using InMemoryCache.Core.Protocol;

namespace InMemoryCache.Client;

// TODO: Попробовать переделать в AsyncDispose и вызывать Connect и Disconnect в конструкторе и Dispose, аналогично переделать и сервер
// TODO: Учесть, что арендованный буффер может быть меньше сообщения и его нужно принимать в цикле

public class TcpClient(int messageMinBytes = 128) : IDisposable
{
  private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

  private readonly int _messageMinBytes = messageMinBytes;

  private bool _disposed;

  public async Task ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
  {
    var parsedIp = IPAddress.Parse(ip);
    var ipAndPort = new IPEndPoint(parsedIp, port);

    await _socket.ConnectAsync(ipAndPort, cancellationToken);
  }

  public async Task DisconnectAsync(CancellationToken cancellationToken = default)
  {
    await _socket.DisconnectAsync(reuseSocket: false, cancellationToken);

    _socket.Shutdown(SocketShutdown.Both);
    _socket.Close();
  }

  public async Task<string> SetAsync(string key, UserProfile profile, CancellationToken cancellationToken = default)
  {
    var value = JsonSerializer.Serialize(profile);
    var command = $"SET {key} {value}";
    var response = await SendAsync(command, cancellationToken);

    return response;
  }

  private async Task<string> SendAsync(string command, CancellationToken cancellationToken)
  {
    var commandBytes = Encoding.UTF8.GetBytes(command);

    await FrameProtocol.SendMessageAsync(_socket, commandBytes, cancellationToken);

    var buffer = ArrayPool<byte>.Shared.Rent(_messageMinBytes);

    try
    {
      var bytesReceived = await FrameProtocol.ReceiveMessageAsync(_socket, buffer, _messageMinBytes, cancellationToken);
      var response = Encoding.UTF8.GetString(buffer.AsSpan(0, bytesReceived));

      return response;
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  public async Task<UserProfile?> GetAsync(string key, CancellationToken cancellationToken = default)
  {
    var command = $"GET {key}";
    var response = await SendAsync(command, cancellationToken);

    if (response.StartsWith("NULL")) return null;

    var profile = JsonSerializer.Deserialize<UserProfile>(response);

    return profile;
  }

  public async Task<string> DeleteAsync(string key, CancellationToken cancellationToken = default)
  {
    var command = $"DEL {key}";
    var response = await SendAsync(command, cancellationToken);

    return response;
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      if (disposing)
      {
        _socket.Dispose();
      }

      _disposed = true;
    }
  }

  void IDisposable.Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
