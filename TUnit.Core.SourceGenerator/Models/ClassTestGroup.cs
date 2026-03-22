namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// A per-class grouping model for generating a TestSource with TestEntry&lt;T&gt; array.
/// Contains only primitives/strings (no ISymbol references) for incremental caching.
/// </summary>
public sealed record ClassTestGroup
{
    public required string ClassFullyQualified { get; init; }
    public required string TestSourceName { get; init; }
    public required EquatableArray<TestMethodSourceCode> Methods { get; init; }

    /// <summary>
    /// Deduplicated attribute factory bodies. Methods with identical attributes share the same index.
    /// </summary>
    public required EquatableArray<string> AttributeGroups { get; init; }

    public required string InstanceFactoryBodyCode { get; init; }
    public required string ReflectionFieldAccessorsCode { get; init; }
    public required string SharedLocalsCode { get; init; }

    /// <summary>
    /// Pre-generated static readonly field declarations for ClassMetadata and classType.
    /// Used to inline MethodMetadata construction into field initializers.
    /// </summary>
    public required string SharedFieldsCode { get; init; }
}
