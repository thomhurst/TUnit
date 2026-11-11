using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Whether TUnit source generation is enabled for the compilation
/// (the <c>EnableTUnitSourceGeneration</c> MSBuild property; enabled unless it is <c>false</c>).
/// </summary>
/// <remarks>
/// A class with two shared instances rather than a <see cref="bool"/>. Incremental pipeline nodes are
/// generic over their element type, and the runtime cannot share generic code across value-type
/// arguments: combining a <c>bool</c> into a pipeline made the compiler process JIT its own copy of
/// Roslyn's node and state-table code for every tuple shape containing it. It converts implicitly to
/// <see cref="bool"/>, so consumers test it as before.
/// </remarks>
internal sealed class GenerationEnabled
{
    private static readonly GenerationEnabled Yes = new();
    private static readonly GenerationEnabled No = new();

    private GenerationEnabled()
    {
    }

    public static implicit operator bool(GenerationEnabled value) => ReferenceEquals(value, Yes);

    public static IncrementalValueProvider<GenerationEnabled> CreateProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ? No : Yes;
            });
    }
}
