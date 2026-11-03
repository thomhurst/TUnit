using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>A generated file: its hint name and text.</summary>
internal readonly record struct GeneratedMockSource(string HintName, string Source);

/// <summary>
/// The sources generated for one model, plus the failure that interrupted generation, if any.
/// Carries no source location, so moving a call site never invalidates generated output; TM009
/// pairs a failure with its request location in a separate step.
/// </summary>
internal sealed record MockEmitResult(
    MockTypeModel Model,
    EquatableArray<GeneratedMockSource> Sources,
    string? FailureExceptionType,
    string? FailureMessage)
{
    public bool Failed => FailureExceptionType is not null;
}

/// <summary>Collects generated files for a model; the incremental pipeline adds them later.</summary>
internal sealed class MockSourceSink
{
    private readonly List<GeneratedMockSource> _sources = new();
    private readonly EquatableArray<string> _externAliases;

    /// <param name="externAliases">Aliases to declare in each source that qualifies a name with one (#7300).</param>
    public MockSourceSink(EquatableArray<string> externAliases) => _externAliases = externAliases;

    public void AddSource(string hintName, string source)
        => _sources.Add(new GeneratedMockSource(hintName, Extensions.ExternAliasQualification.AddExternAliasDirectives(source, _externAliases)));

    public EquatableArray<GeneratedMockSource> ToEquatableArray()
        => _sources.Count == 0
            ? EquatableArray<GeneratedMockSource>.Empty
            : new EquatableArray<GeneratedMockSource>(_sources.ToImmutableArray());
}
