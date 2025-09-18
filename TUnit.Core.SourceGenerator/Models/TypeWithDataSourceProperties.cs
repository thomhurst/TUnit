using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

public struct TypeWithDataSourceProperties
{
    public INamedTypeSymbol TypeSymbol { get; init; }
    public List<PropertyWithDataSource> Properties { get; init; }
}
