using System.Net;
using InMemoryCache.Parser;

namespace InMemoryCache.Log;

public static class Logger
{
  public static void LogClientMessage(EndPoint? clientEndPoint, Command command, byte[] response)
  {
    var responseString = CommandParser.GetString(response).Replace(Environment.NewLine, "");
    var log = $"Client [{clientEndPoint}]. Command received [{command.ToString()}]. Response sent [{responseString}].";

    Console.WriteLine(log);
  }
}
