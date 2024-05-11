using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Models;

internal record ArgumentsContainer
{
    public required Argument[] Arguments { get; init; }
    public required AttributeData? DataAttribute { get; init; }
    public required int? DataAttributeIndex { get; init; }

}