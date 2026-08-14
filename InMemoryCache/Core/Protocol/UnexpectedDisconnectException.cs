namespace InMemoryCache.Core.Protocol;

public class UnexpectedDisconnectException : Exception
{
  public UnexpectedDisconnectException() : base("Socket connection has been disconnected unexpectedly") { }
}
