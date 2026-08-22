namespace InMemoryCache.Generators;

internal class SerializableProperty(string name, string typeName)
{
  internal string Name { get; } = name;
  internal string TypeName { get; } = typeName;
}
