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
        var classMetadataExpression = MetadataGenerationHelper.GenerateClassMetadataGetOrAdd(typeSymbol);
        var methodMetadataCode = MetadataGenerationHelper.GenerateMethodMetadata(methodSymbol, classMetadataExpression);
        
        writer.Append(methodMetadataCode);
    }
}