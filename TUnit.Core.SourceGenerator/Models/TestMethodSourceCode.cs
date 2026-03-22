namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Holds all pre-generated C# code for one test method within a per-class TestSource.
/// All fields are primitives/strings for incremental caching (no ISymbol references).
/// </summary>
public sealed record TestMethodSourceCode
{
    /// <summary>
    /// Safe unique identifier within the class (handles overloads via parameter types).
    /// </summary>
    public required string MethodId { get; init; }

    /// <summary>
    /// Sequential 0-based index of this method within its class.
    /// </summary>
    public required int MethodIndex { get; init; }

    /// <summary>
    /// Pre-generated MethodMetadataFactory.Create(...) expression.
    /// References shared locals (__classMetadata, __classType).
    /// </summary>
    public required string MethodMetadataCode { get; init; }

    /// <summary>
    /// Pre-generated static lambda body for InvokeBody.
    /// Contains the try/catch wrapper around instance.Method().
    /// </summary>
    public required string InvokeBodyCode { get; init; }

    /// <summary>
    /// Pre-generated static lambda body for CreateAttributes.
    /// Returns Attribute[] including data source attributes.
    /// </summary>
    public required string CreateAttributesCode { get; init; }

    /// <summary>
    /// Pre-generated TestEntry data fields: MethodName, FullyQualifiedName, FilePath,
    /// LineNumber, Categories, Properties, DependsOn, HasDataSource, RepeatCount.
    /// </summary>
    public required string TestEntryDataFieldsCode { get; init; }

    /// <summary>
    /// Pre-generated IDataSourceAttribute[] expression for method-level data sources,
    /// or null if empty (no data sources on this test method).
    /// </summary>
    public string? TestDataSourcesCode { get; init; }

    /// <summary>
    /// Pre-generated IDataSourceAttribute[] expression for class-level data sources,
    /// or null if empty (no class-level data sources).
    /// </summary>
    public string? ClassDataSourcesCode { get; init; }

    /// <summary>
    /// Pre-generated TestDependency[] expression, or null if no dependencies.
    /// </summary>
    public string? DependenciesCode { get; init; }
}
