using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestGenerationContext
{
    public required AttributeData TestAttribute { get; init; }
    
    public required AttributeData? ClassDataSourceAttribute { get; init; }
    public required AttributeData? TestDataAttribute { get; init; }
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public required IMethodSymbol MethodSymbol { get; init; }
    public required Argument[] TestArguments { get; init; }
    public required Argument[] ClassArguments { get; init; }
    public required int RepeatIndex { get; init; }
    
    public required int? TestDataAttributeIndex { get; init; }
    public required int? ClassDataAttributeIndex { get; init; }
    
    public required bool HasEnumerableTestMethodData { get; init; }
    public required bool HasEnumerableClassMethodData { get; init; }
}