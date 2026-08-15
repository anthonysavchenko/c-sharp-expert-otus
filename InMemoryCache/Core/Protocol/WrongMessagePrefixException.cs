namespace InMemoryCache.Core.Protocol;

public class WrongMessagePrefixException : Exception
{
  public WrongMessagePrefixException() : base($"Message length prefix has wrong format or value") { }
}
