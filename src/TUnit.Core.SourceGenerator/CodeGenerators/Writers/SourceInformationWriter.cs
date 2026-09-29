using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static void GenerateClassInformation(ICodeWriter sourceCodeWriter, Compilation compilation, INamedTypeSymbol namedTypeSymbol)
    {
        var parentExpression = GenerateParentClassMetadataExpression(namedTypeSymbol, sourceCodeWriter.IndentLevel);
        var classMetadata = MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(namedTypeSymbol, parentExpression, sourceCodeWriter.IndentLevel);

        // Handle multi-line class metadata similar to method metadata
        var lines = classMetadata.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length > 0)
        {
            sourceCodeWriter.Append(lines[0].TrimStart());

            if (lines.Length > 1)
            {
                var secondLine = lines[1];
                var baseIndentSpaces = secondLine.Length - secondLine.TrimStart().Length;

                for (var i = 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]) || i < lines.Length - 1)
                    {
                        sourceCodeWriter.AppendLine();

                        var line = lines[i];
                        var lineIndentSpaces = line.Length - line.TrimStart().Length;
                        var relativeIndent = Math.Max(0, lineIndentSpaces - baseIndentSpaces);
                        var extraIndentLevels = relativeIndent / 4;

                        var trimmedLine = line.TrimStart();
                        for (var j = 0; j < extraIndentLevels; j++)
                        {
                            sourceCodeWriter.Append("    ");
                        }
                        sourceCodeWriter.Append(trimmedLine);
                    }
                }
            }
        }

        sourceCodeWriter.Append(",");
    }

    public static void GenerateMethodInformation(ICodeWriter sourceCodeWriter,
        Compilation compilation, INamedTypeSymbol namedTypeSymbol, IMethodSymbol methodSymbol,
        IDictionary<string, string>? genericSubstitutions, char suffix)
    {
        MetadataGenerationHelper.WriteMethodMetadata(sourceCodeWriter, methodSymbol, namedTypeSymbol);
        sourceCodeWriter.Append($"{suffix}");
        sourceCodeWriter.AppendLine();
    }

    /// <summary>
    /// Recursively generates parent ClassMetadata expression for nested types.
    /// Returns null if the type has no containing type.
    /// </summary>
    private static string? GenerateParentClassMetadataExpression(INamedTypeSymbol typeSymbol, int indentLevel)
    {
        var parent = typeSymbol.ContainingType;
        if (parent == null)
        {
            return null;
        }

        var grandparentExpression = GenerateParentClassMetadataExpression(parent, indentLevel);
        return MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(parent, grandparentExpression, indentLevel);
    }
}
