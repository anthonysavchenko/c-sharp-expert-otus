using System.Net.Sockets;
using System.Text;

namespace InMemoryCache.Core.Protocol;

public static class FrameProtocol
{
  public const int prefixLength = sizeof(int);

  public static async Task<int> ReceiveMessageAsync(
    Socket socket,
    Memory<byte> message,
    int messageMaxLength,
    CancellationToken cancellationToken = default
  )
  {
    var receivedPrefixLength = await ReceiveFramesAsync(socket, message, prefixLength, isDisconnectBeforeReceiveAllowed: true, cancellationToken);

    if (receivedPrefixLength == 0) return 0;

    if (!int.TryParse(message[..prefixLength].Span, out var messageLength)) throw new WrongMessagePrefixException();

    if (messageLength <= 0 || messageLength > messageMaxLength) throw new WrongMessagePrefixException();

    var receivedMessageLength = await ReceiveFramesAsync(socket, message, messageLength, cancellationToken: cancellationToken);

    return receivedMessageLength;
  }

  private static async Task<int> ReceiveFramesAsync(
    Socket socket,
    Memory<byte> message,
    int messageLength,
    bool isDisconnectBeforeReceiveAllowed = false,
    CancellationToken cancellationToken = default
  )
  {
    var receivedFramesLength = 0;

    while (messageLength > receivedFramesLength)
    {
      var emptyBytes = messageLength - receivedFramesLength;
      var buffer = message.Slice(receivedFramesLength, emptyBytes);
      var receivedBytes = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);

      if (receivedBytes == 0 && receivedFramesLength == 0 && isDisconnectBeforeReceiveAllowed) return 0;

      if (receivedBytes == 0) throw new UnexpectedDisconnectException();

      receivedFramesLength += receivedBytes;
    }

    return receivedFramesLength;
  }

  public static async Task SendMessageAsync(Socket socket, byte[] message, CancellationToken cancellationToken = default)
  {
    var messageLength = message.Length.ToString($"D{prefixLength}");
    var commandLength = Encoding.UTF8.GetBytes(messageLength);

    await socket.SendAsync(commandLength, SocketFlags.None, cancellationToken);
    await socket.SendAsync(message, SocketFlags.None, cancellationToken);
  }
}
