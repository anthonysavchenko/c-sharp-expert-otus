using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace InMemoryCache.Generators;

[Generator]
public class SerializerGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValuesProvider<SerializableType?> serializableType = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        "InMemoryCache.Generators.GenerateBinarySerializerAttribute",
        predicate: static (s, _) => true,
        transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx.SemanticModel, ctx.TargetNode)
      )
      .Where(static m => m is not null);

    context.RegisterSourceOutput(serializableType, static (spc, source) => Execute(spc, source));
  }

  static SerializableType? GetSemanticTargetForGeneration(SemanticModel semanticModel, SyntaxNode classDeclarationSyntax)
  {
    if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol) return null;

    var serializableType = BuildSerializableType(classSymbol);

    return serializableType;
  }

  private static SerializableType BuildSerializableType(INamedTypeSymbol classSymbol)
  {
    var props = new List<SerializableProperty>();

    foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (member.DeclaredAccessibility != Accessibility.Public) continue;
      if (member.GetMethod == null) continue;

      props.Add(new SerializableProperty(member.Name, member.Type.ToDisplayString()));
    }

    var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : classSymbol.ContainingNamespace.ToDisplayString();

    return new SerializableType(ns, classSymbol.Name, props);
  }

  static void Execute(SourceProductionContext context, SerializableType? serializableType)
  {
    if (serializableType is { } value)
    {
      string source = SourceCodeGenerator.GeneratePartialClass(context, value);

      context.AddSource($"{value.TypeName}.Serializer.g.cs", SourceText.From(source, Encoding.UTF8));
    }
  }
}
