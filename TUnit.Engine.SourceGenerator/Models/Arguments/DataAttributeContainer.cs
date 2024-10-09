using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal abstract record DataAttributeContainer(ArgumentsType ArgumentsType) : BaseContainer
{
    public required AttributeData? Attribute { get; init; }
    public required int? AttributeIndex { get; init; }
}