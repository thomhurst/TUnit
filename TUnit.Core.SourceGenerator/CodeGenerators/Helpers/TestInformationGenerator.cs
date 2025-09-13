using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TestInformationGenerator
{
    public static void GenerateTestInformation(
        CodeWriter writer,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol typeSymbol)
    {
        var classMetadataExpression = MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(typeSymbol, null, writer.IndentLevel);
        var methodMetadataCode = MetadataGenerationHelper.GenerateMethodMetadata(methodSymbol, classMetadataExpression, writer.IndentLevel);
        
        // Handle multi-line method metadata with proper indentation
        var lines = methodMetadataCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        if (lines.Length > 0)
        {
            writer.Append(lines[0].TrimStart());
            
            if (lines.Length > 1)
            {
                var secondLine = lines[1];
                var baseIndentSpaces = secondLine.Length - secondLine.TrimStart().Length;
                
                for (var i = 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]) || i < lines.Length - 1)
                    {
                        writer.AppendLine();
                        
                        var line = lines[i];
                        var lineIndentSpaces = line.Length - line.TrimStart().Length;
                        var relativeIndent = Math.Max(0, lineIndentSpaces - baseIndentSpaces);
                        var extraIndentLevels = relativeIndent / 4;
                        
                        var trimmedLine = line.TrimStart();
                        for (var j = 0; j < extraIndentLevels; j++)
                        {
                            writer.Append("    ");
                        }
                        writer.Append(trimmedLine);
                    }
                }
            }
        }
    }
}