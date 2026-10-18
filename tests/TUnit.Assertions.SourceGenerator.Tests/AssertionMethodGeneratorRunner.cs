using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Runs <see cref="AssertionMethodGenerator"/> over a single source text, without snapshot verification, for tests
/// that assert on specific parts of its output.
/// </summary>
internal static class AssertionMethodGeneratorRunner
{
    private static readonly Regex ExpectationRegex = new(
        @"protected override string GetExpectation\(\)\s*\{\s*return (?<expectation>[^\r\n]*);");

    /// <summary>The path the input source is parsed with, so diagnostics can point into it.</summary>
    public const string InputPath = "Input.cs";

    /// <summary>Runs the generator over <paramref name="source"/> and returns its generated sources and diagnostics.</summary>
    public static (string[] Sources, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
    {
#if NET
        string[] preprocessorSymbols = ["NET", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER", "NET8_0_OR_GREATER"];
#else
        string[] preprocessorSymbols = [];
#endif
        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols);

        var compilation = CSharpCompilation.Create(
                nameof(AssertionMethodGeneratorRunner),
                [
                    CSharpSyntaxTree.ParseText(source, parseOptions, path: InputPath),
                    CSharpSyntaxTree.ParseText(
                        """
                        global using global::System;
                        global using global::System.Collections.Generic;
                        global using global::System.IO;
                        global using global::System.Linq;
                        global using global::System.Net.Http;
                        global using global::System.Threading;
                        global using global::System.Threading.Tasks;
                        """,
                        parseOptions)
                ],
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var result = CSharpGeneratorDriver.Create(new AssertionMethodGenerator())
            .WithUpdatedParseOptions(parseOptions)
            .RunGenerators(compilation)
            .GetRunResult()
            .Results
            .Single();

        if (result.Exception is not null)
        {
            throw new InvalidOperationException("The generator failed.", result.Exception);
        }

        return (result.GeneratedSources.Select(x => x.SourceText.ToString()).ToArray(), result.Diagnostics);
    }

    /// <summary>The expression each generated <c>GetExpectation()</c> in <paramref name="sources"/> returns.</summary>
    public static string[] Expectations(IEnumerable<string> sources) =>
        sources
            .SelectMany(source => ExpectationRegex.Matches(source).Cast<Match>())
            .Select(match => match.Groups["expectation"].Value)
            .ToArray();
}
