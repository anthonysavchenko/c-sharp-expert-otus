using System.Net;
using InMemoryCache.Core;

namespace InMemoryCache.Log;

public class NumbLogger : ILogger
{
  public void WriteServerLog(ILogWritable server, string message) { }

  public void WriteServerLog(ILogWritable server, Exception exception) { }

  public void WriteClientLog(string? client, string message) { }

  public void WriteClientLog(string? client, Exception exception) { }

  public void WriteClientLog<TLogWritableCommand>(
    EndPoint? clientEndPoint,
    TLogWritableCommand command,
    byte[] response
  )
    where TLogWritableCommand : ILogWritable, allows ref struct
  { }
}
