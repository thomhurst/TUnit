using System.Text.RegularExpressions;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Runs <see cref="AssertionMethodGenerator"/> over every built-in [AssertionFrom] declaration, including the files
/// without a snapshot test, and checks the rendered expectations read as one sentence.
/// </summary>
internal class BuiltInExpectationMessageTests
{
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
        // Guards the test above against passing vacuously, e.g. if the source files stop resolving. The bar sits
        // well below the count on modern .NET because net472 parses the sources without the NET symbols, which
        // drops their #if NET declarations (95 expectations there at the time of writing).
        var expectations = AssertionFromFiles()
            .SelectMany(file => GenerateExpectations(Path.Combine(Git.SourceDirectory.FullName, file)))
            .ToArray();

        await Assert.That(expectations.Length).IsGreaterThan(50);
    }

    /// <summary>
    /// Runs <see cref="AssertionMethodGenerator"/> over <paramref name="file"/> and returns the expression each
    /// generated <c>GetExpectation()</c> returns.
    /// </summary>
    private static string[] GenerateExpectations(string file) =>
        AssertionMethodGeneratorRunner.Expectations(AssertionMethodGeneratorRunner.Run(File.ReadAllText(file)).Sources);
}
