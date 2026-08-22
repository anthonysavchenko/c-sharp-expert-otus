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

    using var memoryStream = new MemoryStream();

    profile.SerializeToBinary(memoryStream);

    var value = memoryStream.ToArray();

    Write(key, value);

    Interlocked.Increment(ref _setCount);
  }

  private void Write(string key, byte[] value)
  {
    void WriterAction() => _storage[key] = value;

    LockedWrite(WriterAction);
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

    var value = Read(key);
    var profile = (UserProfile?)null;

    if (value != null)
    {
      using var memoryStream = new MemoryStream(value);

      profile = new UserProfile();
      profile.DeserializeFromBinary(memoryStream);
    }

    Interlocked.Increment(ref _getCount);

    return profile;
  }

  private byte[]? Read(string key)
  {
    var value = (byte[]?)null;

    void ReaderAction() => value = _storage.GetValueOrDefault(key);

    LockedRead(ReaderAction);

    return value;
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

    bool status = TryDelete(key);

    Interlocked.Increment(ref _deleteCount);

    return status;
  }

  private bool TryDelete(string key)
  {
    bool status = false;

    void WriterAction() => status = _storage.Remove(key);

    LockedWrite(WriterAction);

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
