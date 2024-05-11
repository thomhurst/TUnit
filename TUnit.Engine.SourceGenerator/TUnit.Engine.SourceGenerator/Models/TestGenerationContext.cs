using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestGenerationContext
{
    public required AttributeData TestAttribute { get; init; }
    
    public required AttributeData? ClassDataAttribute { get; init; }
    public required AttributeData? TestDataAttribute { get; init; }
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public required IMethodSymbol MethodSymbol { get; init; }
    public required Argument[] TestArguments { get; init; }
    public required Argument[] ClassArguments { get; init; }
    public required int RepeatCount { get; init; }
    
    public required int? EnumerableTestMethodDataCurrentCount { get; init; }
    public required int? EnumerableClassMethodDataCurrentCount { get; init; }
}