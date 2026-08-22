using System.Text;
using Microsoft.CodeAnalysis;

namespace InMemoryCache.Generators;

public static class SourceCodeGenerator
{
  internal static string GeneratePartialClass(SourceProductionContext context, SerializableType type)
  {
    var sb = new StringBuilder();

    sb.AppendLine("using System;");
    sb.AppendLine("using System.IO;");
    sb.AppendLine();

    if (!string.IsNullOrWhiteSpace(type.Namespace))
    {
      sb.AppendLine("namespace " + type.Namespace + ";");
      sb.AppendLine();
    }

    sb.AppendLine("public partial class " + type.TypeName);
    sb.AppendLine("{");

    AppendSerializeMethod(context, sb, type);
    AppendDeserializeMethod(context, sb, type);

    sb.AppendLine("}");

    var sourceCode = sb.ToString();

    return sourceCode;
  }

  private static void AppendSerializeMethod(SourceProductionContext context, StringBuilder sb, SerializableType type)
  {
    sb.AppendLine("  public void SerializeToBinary(Stream stream)");
    sb.AppendLine("  {");
    sb.AppendLine("    using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);");

    foreach (var property in type.Properties)
    {
      if (!TryAppendPropertyWrite(sb, property)) UnsupportedProperty.Report(context, property, type);
    }

    sb.AppendLine("  }");
  }

  private static bool TryAppendPropertyWrite(StringBuilder sb, SerializableProperty property)
  {
    switch (property.TypeName)
    {
      case "int":
        sb.AppendLine("    writer.Write(this." + property.Name + ");");
        sb.AppendLine();
        return true;

      case "string":
        sb.AppendLine("    {");
        sb.AppendLine("      if (this." + property.Name + " == null)");
        sb.AppendLine("      {");
        sb.AppendLine("        writer.Write(-1);");
        sb.AppendLine("      }");
        sb.AppendLine("      else");
        sb.AppendLine("      {");
        sb.AppendLine("        var bytes = System.Text.Encoding.UTF8.GetBytes(this." + property.Name + ");");
        sb.AppendLine("        var length = bytes.Length;");
        sb.AppendLine();
        sb.AppendLine("        writer.Write(length);");
        sb.AppendLine("        writer.Write(bytes);");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        return true;

      case "System.DateTime":
        sb.AppendLine("    {");
        sb.AppendLine("      var dateTimeAsString = this." + property.Name + ".ToString(\"yyyy-MM-dd HH:mm:ss\");");
        sb.AppendLine("      var bytes = System.Text.Encoding.UTF8.GetBytes(dateTimeAsString);");
        sb.AppendLine("      var length = bytes.Length;");
        sb.AppendLine();
        sb.AppendLine("      writer.Write(length);");
        sb.AppendLine("      writer.Write(bytes);");
        sb.AppendLine("    }");
        sb.AppendLine();
        return true;

      default:
        sb.AppendLine("    // Unsupported type: " + property.TypeName);
        sb.AppendLine();
        return false;
    }
  }

  private static void AppendDeserializeMethod(SourceProductionContext context, StringBuilder sb, SerializableType type)
  {
    sb.AppendLine("  public void DeserializeFromBinary(Stream stream)");
    sb.AppendLine("  {");
    sb.AppendLine("    using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);");

    foreach (var property in type.Properties)
    {
      if (!TryAppendPropertyRead(sb, property)) UnsupportedProperty.Report(context, property, type);
    }

    sb.AppendLine("  }");
  }

  private static bool TryAppendPropertyRead(StringBuilder sb, SerializableProperty property)
  {
    switch (property.TypeName)
    {
      case "int":
        sb.AppendLine("    this." + property.Name + " = reader.ReadInt32();");
        sb.AppendLine();
        return true;

      case "string":
        sb.AppendLine("    {");
        sb.AppendLine("      var length = reader.ReadInt32();");
        sb.AppendLine();
        sb.AppendLine("      if (length == -1)");
        sb.AppendLine("      {");
        sb.AppendLine("        this." + property.Name + " = null;");
        sb.AppendLine("      }");
        sb.AppendLine("      else");
        sb.AppendLine("      {");
        sb.AppendLine("        var bytes = reader.ReadBytes(length);");
        sb.AppendLine("        this." + property.Name + " = System.Text.Encoding.UTF8.GetString(bytes);");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        return true;

      case "System.DateTime":
        sb.AppendLine("    {");
        sb.AppendLine("      var length = reader.ReadInt32();");
        sb.AppendLine("      var bytes = reader.ReadBytes(length);");
        sb.AppendLine("      var dateTimeAsString = System.Text.Encoding.UTF8.GetString(bytes);");
        sb.AppendLine();
        sb.AppendLine("      this." + property.Name + " = DateTime.ParseExact(dateTimeAsString, \"yyyy-MM-dd HH:mm:ss\", System.Globalization.CultureInfo.InvariantCulture);");
        sb.AppendLine("    }");
        sb.AppendLine();
        return true;

      default:
        sb.AppendLine("    // Unsupported type: " + property.TypeName);
        sb.AppendLine();
        return false;
    }
  }
}
