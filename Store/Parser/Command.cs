using System.Text;

namespace Store.Parser;

public readonly ref struct Command
{
  private readonly ReadOnlySpan<byte> _commandType;

  private readonly ReadOnlySpan<byte> _key;

  private readonly ReadOnlySpan<byte> _value;

  public readonly string CommandType
  {
    get => Encoding.UTF8.GetString(_commandType).ToUpperInvariant();
  }

  public readonly string Key
  {
    get => Encoding.UTF8.GetString(_key);
  }

  public readonly byte[] Value
  {
    get => _value.ToArray();
  }

  public Command(ReadOnlySpan<byte> commandType, ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
  {
    _commandType = commandType;
    _key = key;
    _value = value;
  }
}
