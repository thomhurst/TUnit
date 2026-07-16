using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Base class for data source bindings that provide test data.
/// </summary>
public abstract record DataSourceBinding
{
    /// <summary>
    /// The name of the generated delegate method that will provide the data.
    /// </summary>
    public required string GeneratedDelegateMethodName { get; init; }
}

/// <summary>
/// Binding for MethodDataSource attribute.
/// </summary>
public record MethodDataSourceBinding : DataSourceBinding
{
    public required IMethodSymbol SourceMethod { get; init; }
    public required bool IsStatic { get; init; }
    public required INamedTypeSymbol? ContainingType { get; init; }
}

/// <summary>
/// Binding for PropertyDataSource attribute.
/// </summary>
public record PropertyDataSourceBinding : DataSourceBinding
{
    public required IPropertySymbol SourceProperty { get; init; }
    public required bool IsStatic { get; init; }
    public required INamedTypeSymbol? ContainingType { get; init; }
}

/// <summary>
/// Binding for Arguments attribute with compile-time constants.
/// </summary>
public record ArgumentsAttributeBinding : DataSourceBinding
{
    public required IReadOnlyList<TypedConstant> Arguments { get; init; }
}

/// <summary>
/// Binding for GeneratedData attribute.
/// </summary>
public record GeneratedDataBinding : DataSourceBinding
{
    public required ITypeSymbol GeneratorType { get; init; }
}
