using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models.Arguments;

internal abstract record DataAttributeContainer : ArgumentsContainer
{
    public required AttributeData Attribute { get; init; }
    public required int? AttributeIndex { get; init; }
}