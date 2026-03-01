namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// A per-class grouping model for generating shared class helpers.
/// Contains only primitives (no ISymbol references) for incremental caching.
/// </summary>
public sealed record ClassTestGroup
{
    /// <summary>
    /// The fully qualified class name with global:: prefix.
    /// e.g. "global::MyNamespace.MyClass"
    /// </summary>
    public required string ClassFullyQualified { get; init; }

    /// <summary>
    /// The safe identifier name for the class helper.
    /// e.g. "MyNamespace_MyClass__ClassHelper"
    /// </summary>
    public required string ClassHelperName { get; init; }

    /// <summary>
    /// The per-method TestSource class names that should be registered in Initialize().
    /// </summary>
    public required EquatableArray<string> TestSourceClassNames { get; init; }

    /// <summary>
    /// Pre-generated C# code for the CreateInstance method body.
    /// Generated during the transform step where ISymbol is available.
    /// </summary>
    public required string InstanceFactoryBodyCode { get; init; }
}
