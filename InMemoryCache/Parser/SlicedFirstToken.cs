namespace InMemoryCache.Parser;

public readonly ref struct SlicedFirstToken
{
  public readonly ReadOnlySpan<byte> FirstToken;
  public readonly ReadOnlySpan<byte> Rest;

  public SlicedFirstToken(ReadOnlySpan<byte> firstToken, ReadOnlySpan<byte> rest)
  {
    FirstToken = firstToken;
    Rest = rest;
  }
}
