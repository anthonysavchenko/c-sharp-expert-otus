namespace InMemoryCache.Store;

public class SimpleStore : IDisposable
{
  private readonly ReaderWriterLockSlim _lock = new();

  private readonly Dictionary<string, byte[]> _storage = [];

  private long _setCount;

  private long _getCount;

  private long _deleteCount;

  private bool _disposed;

  public void Set(string key, byte[] value)
  {
    ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
    ArgumentNullException.ThrowIfNull(value, nameof(value));
    ArgumentOutOfRangeException.ThrowIfEqual(value.Length, 0, nameof(value));

    void Writer() => _storage[key] = value;

    WriteLocked(Writer);

    Interlocked.Increment(ref _setCount);
  }

  private void WriteLocked(Action writer)
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

  public byte[]? Get(string key)
  {
    ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

    var value = (byte[]?)null;

    void Reader() => value = _storage.GetValueOrDefault(key);

    ReadLocked(Reader);

    Interlocked.Increment(ref _getCount);

    return value;
  }

  private void ReadLocked(Action reader)
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

  public void Delete(string key)
  {
    ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

    void Writer() => _storage.Remove(key);

    WriteLocked(Writer);

    Interlocked.Increment(ref _deleteCount);
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
