using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestGenerationContext
{
    public required AttributeData TestAttribute { get; init; }
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public required IMethodSymbol MethodSymbol { get; init; }
    public required BaseContainer TestArguments { get; init; }
    public required BaseContainer ClassArguments { get; init; }
    public required ClassPropertiesContainer PropertyArguments { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
}