using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

public struct PropertyWithDataSource
{
    public IPropertySymbol Property { get; init; }
    public AttributeData DataSourceAttribute { get; init; }
}