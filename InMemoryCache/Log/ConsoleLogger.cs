using System.Net;
using InMemoryCache.Core;
using InMemoryCache.Parser;

namespace InMemoryCache.Log;

public class ConsoleLogger : ILogger
{
  public void WriteServerLog(ILogWritable server, string message)
  {
    Console.WriteLine($"Server [{server.ToLogString()}]. {message}.");
  }

  public void WriteServerLog(ILogWritable server, Exception exception)
  {
    Console.WriteLine($"Server [{server.ToLogString()}]. Exception occured: {exception}.");
  }

  public void WriteClientLog(string? client, string message)
  {
    Console.WriteLine($"Client [{client}]. {message}.");
  }

  public void WriteClientLog(string? client, Exception exception)
  {
    Console.WriteLine($"Client [{client}]. Exception occured: {exception}.");
  }

  public void WriteClientLog<TLogWritableCommand>(
    EndPoint? clientEndPoint,
    TLogWritableCommand command,
    byte[] response
  )
    where TLogWritableCommand : ILogWritable, allows ref struct
  {
    var responseString = CommandParser.GetString(response).Replace(Environment.NewLine, "");
    var log = $"Client [{clientEndPoint}]. Command received [{command.ToLogString()}]. Response sent [{responseString}].";

    Console.WriteLine(log);
  }
}
