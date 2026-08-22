namespace InMemoryCache.Generators;

internal class SerializableType(string @namespace, string typeName, IReadOnlyList<SerializableProperty> properties)
{
  internal string Namespace { get; } = @namespace;
  internal string TypeName { get; } = typeName;
  internal IReadOnlyList<SerializableProperty> Properties { get; } = properties;
}
