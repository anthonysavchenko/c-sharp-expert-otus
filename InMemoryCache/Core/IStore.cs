namespace InMemoryCache.Core;

public interface IStore : IDisposable
{
  void Set(string key, UserProfile profile);
  UserProfile? Get(string key);
  bool Delete(string key);
}