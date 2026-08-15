using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using InMemoryCache.Core;
using InMemoryCache.Core.Protocol;

namespace InMemoryCache.Client;

public sealed class TcpClient(int messageMinBytes = 128) : IDisposable, IAsyncDisposable
{
  private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

  private readonly int _messageMinBytes = messageMinBytes;

  public static async Task<TcpClient> CreateAsync(
    string serverIp,
    int serverPort,
    int messageMinBytes = 128,
    CancellationToken cancellationToken = default
  )
  {
    var client = new TcpClient(messageMinBytes);

    await client.ConnectAsync(serverIp, serverPort, cancellationToken);

    return client;
  }

  public async Task ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
  {
    var parsedIp = IPAddress.Parse(ip);
    var ipAndPort = new IPEndPoint(parsedIp, port);

    await _socket.ConnectAsync(ipAndPort, cancellationToken);
  }

  public static async Task<string> SetAsync(
    string serverIp,
    int serverPort,
    string key,
    UserProfile profile,
    int messageMinBytes = 128,
    CancellationToken cancellationToken = default
  )
  {
    await using var client = await CreateAsync(serverIp, serverPort, messageMinBytes, cancellationToken);

    var response = await client.SetAsync(key, profile, cancellationToken);

    return response;
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

  public static async Task<UserProfile?> GetAsync(
    string serverIp,
    int serverPort,
    string key,
    int messageMinBytes = 128,
    CancellationToken cancellationToken = default
  )
  {
    await using var client = await CreateAsync(serverIp, serverPort, messageMinBytes, cancellationToken);

    var response = await client.GetAsync(key, cancellationToken);

    return response;
  }

  public async Task<UserProfile?> GetAsync(string key, CancellationToken cancellationToken = default)
  {
    var command = $"GET {key}";
    var response = await SendAsync(command, cancellationToken);

    if (response.StartsWith("NULL")) return null;

    var profile = JsonSerializer.Deserialize<UserProfile>(response);

    return profile;
  }

  public static async Task<string> DeleteAsync(
    string serverIp,
    int serverPort,
    string key,
    int messageMinBytes = 128,
    CancellationToken cancellationToken = default
  )
  {
    await using var client = await CreateAsync(serverIp, serverPort, messageMinBytes, cancellationToken);

    var response = await client.DeleteAsync(key, cancellationToken);

    return response;
  }

  public async Task<string> DeleteAsync(string key, CancellationToken cancellationToken = default)
  {
    var command = $"DEL {key}";
    var response = await SendAsync(command, cancellationToken);

    return response;
  }

  public async Task DisconnectAsync(CancellationToken cancellationToken = default)
  {
    _socket.Shutdown(SocketShutdown.Both);

    await _socket.DisconnectAsync(reuseSocket: false, cancellationToken);

    _socket.Close();
  }

  public void Dispose()
  {
    _socket.Shutdown(SocketShutdown.Both);
    _socket.Disconnect(reuseSocket: false);
    _socket.Dispose();
  }

  public async ValueTask DisposeAsync()
  {
    _socket.Shutdown(SocketShutdown.Both);

    await _socket.DisconnectAsync(reuseSocket: false).ConfigureAwait(false);

    _socket.Dispose();
  }
}
