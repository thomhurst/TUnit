using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Enums;

namespace TUnit.Core.SourceGenerator.Models.Arguments;

public abstract record DataSourceAttributeContainer(ArgumentsType ArgumentsType) : BaseContainer
{
    public override required AttributeData? Attribute { get; init; }
    public required int? AttributeIndex { get; init; }
}