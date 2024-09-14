using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ArgumentsContainer
{
    public required Argument[] Arguments { get; init; }
    public required AttributeData? DataAttribute { get; init; }
    public required bool IsEnumerableData { get; init; }
    public required int? DataAttributeIndex { get; init; }
    public string? ConstructorCommand { get; init; }

    public bool HasData()
    {
        return ConstructorCommand != null || Arguments.Any();
    }
}