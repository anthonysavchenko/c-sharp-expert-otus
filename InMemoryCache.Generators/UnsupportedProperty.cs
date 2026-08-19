using Microsoft.CodeAnalysis;

namespace InMemoryCache.Generators;

internal static class UnsupportedProperty
{
  internal static void Report(
    SourceProductionContext context,
    SerializableProperty prop,
    SerializableType type
  )
  {
    var diagnostic = Diagnostic.Create(
      UnsupportedPropertyDescriptor,
      location: null,
      type.TypeName,
      prop.TypeName,
      prop.Name
    );

    context.ReportDiagnostic(diagnostic);
  }

  private static readonly DiagnosticDescriptor UnsupportedPropertyDescriptor = new(
    id: "MEMC0001",
    title: "Unsupported property type",
    messageFormat: "Property '{2}' in type '{0}' has unsupported type for binary serialization: '{1}'",
    category: "SerializerGenerator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );
}
