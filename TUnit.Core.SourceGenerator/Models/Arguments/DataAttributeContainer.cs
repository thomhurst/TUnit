using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Arguments;

public abstract record DataAttributeContainer(ArgumentsType ArgumentsType) : BaseContainer
{
    public required AttributeData? Attribute { get; init; }
    public required int? AttributeIndex { get; init; }
}