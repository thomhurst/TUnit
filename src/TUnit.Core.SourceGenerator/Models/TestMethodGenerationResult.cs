using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// The generated output for one [Test] method (or one inherited test method), produced while symbols are
/// available and carried through the incremental pipeline as strings only.
/// Exactly one of <see cref="PerClassMethod"/>, <see cref="Source"/> or <see cref="Error"/> is set.
/// </summary>
/// <remarks>
/// Value equality lets Roslyn skip the grouping and output steps when an edit does not change the
/// code a test produces, instead of regenerating every test on every compilation.
/// </remarks>
public sealed record TestMethodGenerationResult
{
    /// <summary>Pre-generated code for a non-generic test declared directly on its class, emitted in the per-class TestSource.</summary>
    public PerClassTestMethodCode? PerClassMethod { get; init; }

    /// <summary>A complete standalone source file (generic and inherited tests).</summary>
    public GeneratedTestSource? Source { get; init; }

    /// <summary>A generation failure to report instead of a source file.</summary>
    public TestGenerationError? Error { get; init; }
}

/// <summary>
/// Pre-generated code for one test method in a per-class TestSource, plus the class-level code shared by
/// every method of that class (the grouping step takes the class-level code from the first method).
/// </summary>
public sealed record PerClassTestMethodCode
{
    public required string ClassFullyQualified { get; init; }
    public required string TestSourceName { get; init; }
    public required string InstanceFactoryBodyCode { get; init; }
    public required string ReflectionFieldAccessorsCode { get; init; }
    public required string SharedFieldsCode { get; init; }

    /// <summary>Attribute factory body, deduplicated across the class by the grouping step.</summary>
    public required string AttributesCode { get; init; }

    public required string MethodMetadataArgumentsCode { get; init; }

    /// <summary>The invoke switch case body, without the <c>case N:</c> label (the index is assigned when grouping).</summary>
    public required string InvokeBodyCode { get; init; }

    public required string TestEntryDataFieldsCode { get; init; }
    public string? TestDataSourcesCode { get; init; }
    public string? ClassDataSourcesCode { get; init; }
    public string? DependenciesCode { get; init; }
}

public sealed record GeneratedTestSource(string HintName, string SourceCode);

/// <summary>
/// A source generation failure, stored without a <see cref="Location"/> so no syntax tree is retained.
/// </summary>
public sealed record TestGenerationError(
    string ClassName,
    string MethodName,
    string Details,
    string? FilePath,
    TextSpan Span,
    LinePositionSpan LineSpan)
{
    public static TestGenerationError Create(string className, string methodName, string details, Location? location)
    {
        if (location is { IsInSource: true })
        {
            var lineSpan = location.GetLineSpan();
            return new TestGenerationError(className, methodName, details, lineSpan.Path, location.SourceSpan, lineSpan.Span);
        }

        return new TestGenerationError(className, methodName, details, null, default, default);
    }

    public Location ToLocation() =>
        FilePath is null ? Location.None : Location.Create(FilePath, Span, LineSpan);
}

/// <summary>
/// The generated output for one [InheritsTests] class: one result per inherited test method.
/// </summary>
public sealed record InheritsTestsClassResult(EquatableArray<TestMethodGenerationResult> Methods);
