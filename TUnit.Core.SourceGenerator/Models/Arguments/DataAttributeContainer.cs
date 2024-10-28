using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public abstract record DataAttributeContainer(ArgumentsType ArgumentsType) : BaseContainer
{
    public required AttributeData? Attribute { get; init; }
    public required int? AttributeIndex { get; init; }
}