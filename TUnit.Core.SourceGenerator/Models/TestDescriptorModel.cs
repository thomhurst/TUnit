using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Base model for test descriptors containing common test metadata.
/// </summary>
public abstract record TestDescriptorModel
{
    public required string TestId { get; init; }
    public required string DisplayName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required bool IsSkipped { get; init; }
    public string? SkipReason { get; init; }
    public TimeSpan? Timeout { get; init; }
    public required int RepeatCount { get; init; }
}

/// <summary>
/// Model representing a test method with its metadata and data sources.
/// </summary>
public record TestMethodModel : TestDescriptorModel
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol ContainingType { get; init; }
    public required bool IsAsync { get; init; }
    public DataSourceBinding? MethodDataSource { get; init; }
    public DataSourceBinding? ClassDataSource { get; init; }
    public IReadOnlyDictionary<string, DataSourceBinding>? PropertyDataSources { get; init; }
}

/// <summary>
/// Model representing a test class with its test methods.
/// </summary>
public record TestClassModel
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required IReadOnlyList<TestMethodModel> TestMethods { get; init; }
    public required bool IsGeneric { get; init; }
    public IMethodSymbol? SelectedConstructor { get; init; }
    public required bool HasParameterlessConstructor { get; init; }
    public required IReadOnlyList<IPropertySymbol> RequiredProperties { get; init; }
}