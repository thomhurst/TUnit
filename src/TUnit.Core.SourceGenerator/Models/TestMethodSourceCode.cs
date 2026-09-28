namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Holds all pre-generated C# code for one test method within a per-class TestSource.
/// All fields are primitives/strings for incremental caching (no ISymbol references).
/// </summary>
public sealed record TestMethodSourceCode
{
    public required int MethodIndex { get; init; }
    public required int AttributeGroupIndex { get; init; }

    /// <summary>Named TestEntryFactory arguments describing the method (return type, generic arity, parameters).</summary>
    public required string MethodMetadataArgumentsCode { get; init; }

    /// <summary>Block for the method's case in the class-level __Invoke switch, without the <c>case N:</c> label.</summary>
    public required string InvokeBodyCode { get; init; }

    /// <summary>TestEntry data fields (MethodName, FilePath, etc.).</summary>
    public required string TestEntryDataFieldsCode { get; init; }

    public string? TestDataSourcesCode { get; init; }
    public string? ClassDataSourcesCode { get; init; }
    public string? DependenciesCode { get; init; }
}
