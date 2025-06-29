using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Contains all the metadata about a test method discovered by the source generator.
/// </summary>
public class TestMethodMetadata
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required AttributeData TestAttribute { get; init; }
    public required GeneratorAttributeSyntaxContext Context { get; init; }
}
