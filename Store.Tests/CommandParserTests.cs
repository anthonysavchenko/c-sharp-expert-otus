using Store.Parser;

namespace Store.Tests;

public class CommandParserTests
{
  [Fact]
  public void CorrectSet()
  {
    var (commandType, key, value) = Parse("SET user:1 data");

    Assert.Equal("SET", commandType);
    Assert.Equal("user:1", key);
    Assert.Equal("data", value);
  }

  [Fact]
  public void CorrectGet()
  {
    var (commandType, key, value) = Parse("GET user:1");

    Assert.Equal("GET", commandType);
    Assert.Equal("user:1", key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void IncorrectGet_NoKey()
  {
    var (commandType, key, value) = Parse("GET");

    Assert.Equal(string.Empty, commandType);
    Assert.Equal(string.Empty, key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void IncorrectSet_NoCommandType()
  {
    var (commandType, key, value) = Parse(" user:1 data");

    Assert.Equal(string.Empty, commandType);
    Assert.Equal(string.Empty, key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void Incorrect_Empty()
  {
    var (commandType, key, value) = Parse("");

    Assert.Equal(string.Empty, commandType);
    Assert.Equal(string.Empty, key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void Incorrect_SpacesOnly()
  {
    var (commandType, key, value) = Parse("  ");

    Assert.Equal(string.Empty, commandType);
    Assert.Equal(string.Empty, key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void IncorrectSet_TooManySpacesAfterCommandType()
  {
    var (commandType, key, value) = Parse("SET        user:1 data");

    Assert.Equal(string.Empty, commandType);
    Assert.Equal(string.Empty, key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void IncorrectSet_TooManySpacesAfterKey()
  {
    var (commandType, key, value) = Parse("SET user:1         data");

    Assert.Equal("SET", commandType);
    Assert.Equal("user:1", key);
    Assert.Equal(string.Empty, value);
  }

  [Fact]
  public void IncorrectGet_NoKeyExtraSpace()
  {
    var (commandType, key, value) = Parse("GET ");

    Assert.Equal(string.Empty, commandType);
    Assert.Equal(string.Empty, key);
    Assert.Equal(string.Empty, value);
  }

  private static (string, string, string) Parse(string message)
  {
    var bytes = CommandParser.GetBytes(message);
    var command = CommandParser.ParseBytes(bytes);
    var value = CommandParser.GetString(command.Value);

    return (command.CommandType, command.Key, value);
  }
}
