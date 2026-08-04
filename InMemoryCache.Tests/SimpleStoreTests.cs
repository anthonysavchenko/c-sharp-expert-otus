using System.Text;
using InMemoryCache.Core;
using InMemoryCache.Parser;
using InMemoryCache.Store;

namespace InMemoryCache.Tests;

public class SimpleStoreTests
{
  [Fact]
  public void IncorrectSet_NullKey()
  {
    using var store = new SimpleStore();
    var key = (string?)null;
    var value = new UserProfile();

    void Action() => store.Set(key!, value);

    Assert.Throws<ArgumentNullException>(Action);
  }

  [Fact]
  public void IncorrectSet_EmptyKey()
  {
    using var store = new SimpleStore();
    var key = string.Empty;
    var value = new UserProfile();

    void Action() => store.Set(key, value);

    Assert.Throws<ArgumentException>(Action);
  }

  [Fact]
  public void IncorrectSet_NullValue()
  {
    using var store = new SimpleStore();
    var key = "user:1";
    var value = (UserProfile?)null;

    void Action() => store.Set(key, value!);

    Assert.Throws<ArgumentNullException>(Action);
  }

  [Fact]
  public void IncorrectGet_NullKey()
  {
    using var store = new SimpleStore();
    var key = (string?)null;

    void Action() => store.Get(key!);

    Assert.Throws<ArgumentNullException>(Action);
  }

  [Fact]
  public void IncorrectGet_EmptyKey()
  {
    using var store = new SimpleStore();
    var key = string.Empty;

    void Action() => store.Get(key);

    Assert.Throws<ArgumentException>(Action);
  }

  [Fact]
  public void IncorrectDelete_NullKey()
  {
    using var store = new SimpleStore();
    var key = (string?)null;

    void Action() => store.Delete(key!);

    Assert.Throws<ArgumentNullException>(Action);
  }

  [Fact]
  public void IncorrectDelete_EmptyKey()
  {
    using var store = new SimpleStore();
    var key = string.Empty;

    void Action() => store.Delete(key);

    Assert.Throws<ArgumentException>(Action);
  }

  [Fact]
  public void IncorrectDelete_KeyWasNotFound()
  {
    using var store = new SimpleStore();
    var key = "user:1";

    var status = store.Delete(key);

    Assert.False(status);
  }

  [Fact]
  public void CorrectSetGetDelete()
  {
    using var store = new SimpleStore();
    var key = "user:1";

    var value = new UserProfile()
    {
      Id = 1000,
      Username = "John Smith",
      CreatedAt = new DateTime(2026, 8, 1),
    };

    store.Set(key, value);
    var valueFromStore = store.Get(key);
    var deleteStatus = store.Delete(key);
    var valueFromStoreAfterDelete = store.Get(key);

    var ddMMyyyyFormat = "dd.MM.yyyy";

    Assert.NotNull(valueFromStore);
    Assert.Equal(value.Id, valueFromStore.Id);
    Assert.Equal(value.Username, valueFromStore.Username);
    Assert.Equal(value.CreatedAt.ToString(ddMMyyyyFormat), valueFromStore.CreatedAt.ToString(ddMMyyyyFormat));
    Assert.True(deleteStatus);
    Assert.Null(valueFromStoreAfterDelete);
  }

  [Fact]
  public async Task CorrectSetGetDeleteAsync()
  {
    using var store = new SimpleStore();

    var copyFromKey = "user:1";
    var copyToKey = "user:2";
    var value = new UserProfile() { Username = "John Smith" };
    var count = 10;

    store.Set(copyFromKey, value);

    var tasks = ArrangeTasks(store, copyFromKey, copyToKey, count);

    await Task.WhenAll(tasks);

    var valueFromStoreCopyFrom = store.Get(copyFromKey);

    Assert.NotNull(valueFromStoreCopyFrom);
    Assert.Equal(value.Username, valueFromStoreCopyFrom.Username);

    var valueFromStoreCopyTo = store.Get(copyToKey);

    Assert.NotNull(valueFromStoreCopyTo);
    Assert.Equal(value.Username, valueFromStoreCopyTo.Username);

    store.Delete(copyFromKey);
    store.Delete(copyToKey);

    var (setCount, getCount, deleteCount) = store.GetStatistics();

    Assert.Equal(count + 1, setCount);
    Assert.Equal(count + 2, getCount);
    Assert.Equal(2, deleteCount);
  }

  public IEnumerable<Task> ArrangeTasks(SimpleStore store, string copyFromKey, string copyToKey, int count)
  {
    var tasks = new List<Task>();

    void Action() => CopyStoreValue(store, copyFromKey, copyToKey);

    for (int i = 0; i < count; i++)
    {
      var task = Task.Run(Action, TestContext.Current.CancellationToken);

      tasks.Add(task);
    }

    return tasks;
  }

  private static void CopyStoreValue(SimpleStore store, string copyFromKey, string copyToKey)
  {
    var value = store.Get(copyFromKey);

    store.Set(copyToKey, value!);
  }
}
