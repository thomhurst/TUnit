using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class SourceInformationWriter
{
    public static void GenerateClassInformation(ICodeWriter sourceCodeWriter, Compilation compilation, INamedTypeSymbol namedTypeSymbol)
    {
        var parent = namedTypeSymbol.ContainingType;
        var parentExpression = parent != null ? MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(parent, null, sourceCodeWriter.IndentLevel) : null;
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
                
                for (int i = 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]) || i < lines.Length - 1)
                    {
                        sourceCodeWriter.AppendLine();
                        
                        var line = lines[i];
                        var lineIndentSpaces = line.Length - line.TrimStart().Length;
                        var relativeIndent = Math.Max(0, lineIndentSpaces - baseIndentSpaces);
                        var extraIndentLevels = relativeIndent / 4;
                        
                        var trimmedLine = line.TrimStart();
                        for (int j = 0; j < extraIndentLevels; j++)
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

    private static void GenerateAssemblyInformation(ICodeWriter sourceCodeWriter, Compilation compilation, IAssemblySymbol assembly)
    {
        var assemblyMetadata = MetadataGenerationHelper.GenerateAssemblyMetadataGetOrAdd(assembly);
        sourceCodeWriter.Append(assemblyMetadata);
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

    public static void GenerateMembers(ICodeWriter sourceCodeWriter, Compilation compilation, INamedTypeSymbol namedTypeSymbol, ImmutableArray<IParameterSymbol> parameters, IPropertySymbol? property, ArgumentsType argumentsType)
    {
        if (parameters.Length == 0 && property is null)
        {
            sourceCodeWriter.Append("[],");
            return;
        }

        sourceCodeWriter.Append("[");

        if (property is not null)
        {
            GeneratePropertyInformation(sourceCodeWriter, compilation, property, namedTypeSymbol);
        }

        foreach (var parameter in parameters)
        {
            GenerateParameterInformation(sourceCodeWriter, compilation, parameter, argumentsType, null);
        }

        sourceCodeWriter.Append("],");
    }

    public static void GeneratePropertyInformation(ICodeWriter sourceCodeWriter,
        Compilation compilation, IPropertySymbol property, INamedTypeSymbol namedTypeSymbol)
    {
        MetadataGenerationHelper.WritePropertyMetadata(sourceCodeWriter, property, namedTypeSymbol);
        sourceCodeWriter.Append(",");
    }

    public static void GenerateParameterInformation(ICodeWriter sourceCodeWriter,
        Compilation context,
        IParameterSymbol parameter, ArgumentsType argumentsType,
        IDictionary<string, string>? genericSubstitutions)
    {
        // For now, use the generic version since it's what the existing code was doing
        MetadataGenerationHelper.WriteParameterMetadataGeneric(sourceCodeWriter, parameter);
        sourceCodeWriter.Append(",");
    }
}
