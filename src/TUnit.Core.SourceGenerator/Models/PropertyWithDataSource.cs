using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

public record PropertyWithDataSource
{
    public required IPropertySymbol Property { get; init; }
    public required AttributeData DataSourceAttribute { get; init; }
}

public record PropertyWithDataSourceModel(PropertyType Property, DataSourceAttribute DataSourceAttribute);

public record ContainingType(string GloballyQualified, string Name, string ContainingNamespace, string ContainingAssemblyName);

public record PropertyType(string GloballyQualifiedType, string Name, ContainingType ContainingType);

public abstract record DataSourceAttribute
{
    public record ArgumentsDataSource(string? FormattedValue) : DataSourceAttribute;
    public record MethodDataSource(string? Data) : DataSourceAttribute;
    public record AsyncDataSource(string GeneratedCode) : DataSourceAttribute;
    public record Fallback : DataSourceAttribute;
}
