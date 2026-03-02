namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// A per-class grouping model for generating a consolidated TestSource.
/// Contains only primitives/strings (no ISymbol references) for incremental caching.
/// </summary>
public sealed record ClassTestGroup
{
    /// <summary>
    /// The fully qualified class name with global:: prefix.
    /// e.g. "global::MyNamespace.MyClass"
    /// </summary>
    public required string ClassFullyQualified { get; init; }

    /// <summary>
    /// The safe identifier name for the per-class TestSource.
    /// e.g. "MyNamespace_MyClass__TestSource"
    /// </summary>
    public required string TestSourceName { get; init; }

    /// <summary>
    /// Pre-generated code for each test method in this class.
    /// </summary>
    public required EquatableArray<TestMethodSourceCode> Methods { get; init; }

    /// <summary>
    /// Deduplicated attribute factory method bodies.
    /// Index corresponds to TestMethodSourceCode.AttributeGroupIndex.
    /// Methods with identical attributes share the same body.
    /// </summary>
    public required EquatableArray<string> AttributeGroups { get; init; }

    /// <summary>
    /// Pre-generated C# code for the CreateInstance method body.
    /// Generated during the transform step where ISymbol is available.
    /// </summary>
    public required string InstanceFactoryBodyCode { get; init; }

    /// <summary>
    /// Pre-generated UnsafeAccessor declarations for init-only properties with data sources.
    /// Empty string if none needed.
    /// </summary>
    public required string ReflectionFieldAccessorsCode { get; init; }
}
