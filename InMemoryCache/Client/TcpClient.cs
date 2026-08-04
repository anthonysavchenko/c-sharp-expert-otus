using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InMemoryCache.Client;

// TODO: Попробовать переделать в AsyncDispose и вызывать Connect и Disconnect в конструкторе и Dispose, аналогично переделать и сервер
// TODO: Передавать в команде Get количество символов и отрезать лишнее при получении

public class TcpClient(int messageMinBytes = 64) : IDisposable
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

  public async Task<string> SetAsync(string key, string value, CancellationToken cancellationToken = default)
  {
    var command = $"SET {key} {value}";

    var response = await SendAsync(command, cancellationToken);

    return response;
  }

  private async Task<string> SendAsync(string command, CancellationToken cancellationToken)
  {
    var commandBytes = Encoding.UTF8.GetBytes(command);

    await _socket.SendAsync(commandBytes, SocketFlags.None, cancellationToken);

    var responseBytes = new byte[_messageMinBytes];

    await _socket.ReceiveAsync(responseBytes, SocketFlags.None, cancellationToken);

    var response = Encoding.UTF8.GetString(responseBytes);

    return response;
  }

  public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
  {
    var command = $"GET {key}";

    var response = await SendAsync(command, cancellationToken);

    return response;
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
