using System.Text;

namespace Store.Parser;

public static class CommandParser
{
  public const string SetCommandType = "SET";

  public const string GetCommandType = "GET";

  public const string DeleteCommandType = "DEL";

  public static string GetString(ReadOnlySpan<byte> input) => Encoding.UTF8.GetString(input);

  public static byte[] GetBytes(string input) => Encoding.UTF8.GetBytes(input);

  private static readonly byte[] _separator = GetBytes(" ");

  public static Command ParseBytes(ReadOnlySpan<byte> bytes)
  {
    var parsedCommandType = SliceFirstToken(bytes);
    var parsedKey = SliceFirstToken(parsedCommandType.Rest);
    var parsedValue = SliceFirstToken(parsedKey.Rest);

    var commandType = parsedCommandType.FirstToken;
    var key = parsedKey.FirstToken;
    var value = parsedValue.FirstToken;

    if (commandType.IsEmpty || key.IsEmpty) return new Command(commandType: [], key: [], value: []);

    var command = new Command(commandType, key, value);

    return command;
  }

  private static SlicedFirstToken SliceFirstToken(ReadOnlySpan<byte> bytes)
  {
    if (bytes.IsEmpty) return new SlicedFirstToken(firstToken: [], rest: []);

    var separatorStartIndex = bytes.IndexOf(_separator);

    if (separatorStartIndex == -1) return new SlicedFirstToken(firstToken: bytes, rest: []);

    var firstToken = bytes[..separatorStartIndex];
    var restStartIndex = separatorStartIndex + _separator.Length;

    if (restStartIndex == bytes.Length) return new SlicedFirstToken(firstToken, rest: []);

    var rest = bytes[restStartIndex..];
    var slicedFirstToken = new SlicedFirstToken(firstToken, rest);

    return slicedFirstToken;
  }
}
