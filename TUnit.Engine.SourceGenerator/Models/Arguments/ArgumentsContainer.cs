using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal record ArgumentsContainer
{
    public required Argument[] Arguments { get; init; }
    public required AttributeData? DataAttribute { get; init; }
    public required bool IsEnumerableData { get; init; }
    public required int? DataAttributeIndex { get; init; }
    public string? ClassConstructorType { get; init; }

    public bool HasData()
    {
        return ClassConstructorType != null || Arguments.Any();
    }
}