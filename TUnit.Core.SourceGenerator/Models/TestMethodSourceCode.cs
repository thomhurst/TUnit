namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Holds all pre-generated C# code for one test method within a per-class TestSource.
/// All fields are primitives/strings for incremental caching (no ISymbol references).
/// </summary>
public sealed record TestMethodSourceCode
{
    public required string MethodId { get; init; }
    public required int MethodIndex { get; init; }
    public required int AttributeGroupIndex { get; init; }
    public required string MethodMetadataCode { get; init; }

    /// <summary>Switch case body for the class-level __Invoke method.</summary>
    public required string InvokeSwitchCaseCode { get; init; }

    /// <summary>TestEntry data fields (MethodName, FilePath, etc.).</summary>
    public required string TestEntryDataFieldsCode { get; init; }

    public string? TestDataSourcesCode { get; init; }
    public string? ClassDataSourcesCode { get; init; }
    public string? DependenciesCode { get; init; }
}
