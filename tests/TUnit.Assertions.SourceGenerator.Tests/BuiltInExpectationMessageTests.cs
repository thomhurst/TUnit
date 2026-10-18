using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Runs <see cref="AssertionMethodGenerator"/> over every built-in [AssertionFrom] declaration, including the files
/// without a snapshot test, and checks the rendered expectations read as one sentence.
/// </summary>
internal class BuiltInExpectationMessageTests
{
    private static readonly Regex ExpectationRegex = new(
        @"protected override string GetExpectation\(\)\s*\{\s*return (?<expectation>[^\r\n]*);");

    private static readonly Regex RepeatedWordRegex = new(
        @"\b(to|not) \1\b|\{\(_negated \? ""not "" : """"\)\}(to|not) ");

    /// <summary>
    /// The source files under <c>src/TUnit.Assertions</c> that declare [AssertionFrom], relative to <c>src</c> so
    /// test names stay the same on every machine.
    /// </summary>
    public static IEnumerable<string> AssertionFromFiles() =>
        Directory.EnumerateFiles(Path.Combine(Git.SourceDirectory.FullName, "TUnit.Assertions"), "*.cs", SearchOption.AllDirectories)
            .Where(file => File.ReadLines(file).Any(line => line.TrimStart().StartsWith("[AssertionFrom", StringComparison.Ordinal)))
            .Select(file => file.Substring(Git.SourceDirectory.FullName.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .OrderBy(file => file, StringComparer.Ordinal);

    [Test]
    [MethodDataSource(nameof(AssertionFromFiles))]
    public async Task Expectations_Do_Not_Repeat_To_Or_Not(string file)
    {
        var expectations = GenerateExpectations(Path.Combine(Git.SourceDirectory.FullName, file));

        await Assert.That(expectations.Where(expectation => RepeatedWordRegex.IsMatch(expectation))).IsEmpty();
    }

    [Test]
    public async Task Built_In_Declarations_Render_Expectations()
    {
        // Guards the test above against passing vacuously, e.g. if the source files stop resolving.
        var expectations = AssertionFromFiles()
            .SelectMany(file => GenerateExpectations(Path.Combine(Git.SourceDirectory.FullName, file)))
            .ToArray();

        await Assert.That(expectations.Length).IsGreaterThan(100);
    }

    /// <summary>
    /// Runs <see cref="AssertionMethodGenerator"/> over <paramref name="file"/> and returns the expression each
    /// generated <c>GetExpectation()</c> returns.
    /// </summary>
    private static string[] GenerateExpectations(string file)
    {
#if NET
        string[] preprocessorSymbols = ["NET", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER", "NET8_0_OR_GREATER"];
#else
        string[] preprocessorSymbols = [];
#endif
        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(preprocessorSymbols);

        var compilation = CSharpCompilation.Create(
                nameof(BuiltInExpectationMessageTests),
                [
                    CSharpSyntaxTree.ParseText(File.ReadAllText(file), parseOptions),
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
            throw new InvalidOperationException($"The generator failed for {file}.", result.Exception);
        }

        return result.GeneratedSources
            .SelectMany(source => ExpectationRegex.Matches(source.SourceText.ToString()).Cast<Match>())
            .Select(match => match.Groups["expectation"].Value)
            .ToArray();
    }
}
