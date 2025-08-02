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
        var parentExpression = parent != null ? MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(parent) : null;
        var classMetadata = MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(namedTypeSymbol, parentExpression);
        sourceCodeWriter.Append(classMetadata);
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
        var classMetadataExpression = MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(namedTypeSymbol);
        var methodMetadata = MetadataGenerationHelper.GenerateMethodMetadata(methodSymbol, classMetadataExpression);
        sourceCodeWriter.Append(methodMetadata);
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
        var propertyMetadata = MetadataGenerationHelper.GeneratePropertyMetadata(property, namedTypeSymbol);
        sourceCodeWriter.Append(propertyMetadata);
        sourceCodeWriter.Append(",");
    }

    public static void GenerateParameterInformation(ICodeWriter sourceCodeWriter,
        Compilation context,
        IParameterSymbol parameter, ArgumentsType argumentsType,
        IDictionary<string, string>? genericSubstitutions)
    {
        // For now, use the generic version since it's what the existing code was doing
        var parameterMetadata = MetadataGenerationHelper.GenerateParameterMetadataGeneric(parameter);
        sourceCodeWriter.Append(parameterMetadata);
        sourceCodeWriter.Append(",");
    }
}
