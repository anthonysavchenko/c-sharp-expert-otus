using System.Text.Json;
using InMemoryCache.Core;

namespace InMemoryCache.Store;

public class SimpleStore : IStore
{
  private readonly ReaderWriterLockSlim _lock = new();

  private readonly Dictionary<string, byte[]> _storage = [];

  private long _setCount;

  private long _getCount;

  private long _deleteCount;

  private bool _disposed;

  public void Set(string key, UserProfile profile)
  {
    ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
    ArgumentNullException.ThrowIfNull(profile, nameof(profile));

    var value = JsonSerializer.SerializeToUtf8Bytes(profile);

    void Writer() => _storage[key] = value;

    LockedWrite(Writer);

    Interlocked.Increment(ref _setCount);
  }

  private void LockedWrite(Action writer)
  {
    _lock.EnterWriteLock();

    try
    {
      writer.Invoke();
    }
    finally
    {
      _lock.ExitWriteLock();
    }
  }

  public UserProfile? Get(string key)
  {
    ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

    var value = (byte[]?)null;

    void Reader() => value = _storage.GetValueOrDefault(key);

    LockedRead(Reader);

    var profile = JsonSerializer.Deserialize<UserProfile>(value);

    Interlocked.Increment(ref _getCount);

    return profile;
  }

  private void LockedRead(Action reader)
  {
    _lock.EnterReadLock();

    try
    {
      reader.Invoke();
    }
    finally
    {
      _lock.ExitReadLock();
    }
  }

  public bool Delete(string key)
  {
    ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

    bool status = false;

    void Writer() => status = _storage.Remove(key);

    LockedWrite(Writer);

    Interlocked.Increment(ref _deleteCount);

    return status;
  }

  public (long, long, long) GetStatistics()
  {
    var setCount = Interlocked.Read(ref _setCount);
    var getCount = Interlocked.Read(ref _getCount);
    var deleteCount = Interlocked.Read(ref _deleteCount);

    return (setCount, getCount, deleteCount);
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      if (disposing)
      {
        _lock.Dispose();
      }

      _disposed = true;
    }
  }
}
