using TUnit.Tests.Shared;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class TupleConversionMatrixTests : BaseAnalyzerTests
{
    public static IEnumerable<string> Names() => TupleConversionMatrix.Cases.Select(row => row.Name);

    [Test, MethodDataSource(nameof(Names))]
    public async Task AnalyzerPolicy(string name)
    {
        var row = TupleConversionMatrix.Cases.Single(row => row.Name == name);
        var fixture = TupleConversionMatrix.Fixture(row);
        if (!row.Accepted)
        {
            fixture = fixture.Replace("MethodDataSource(nameof(Row))", "{|#0:MethodDataSource(nameof(Row))|}")
                .Replace("MethodDataSource(nameof(Rows))", "{|#1:MethodDataSource(nameof(Rows))|}");
        }

        var source = "using System; using System.Collections.Generic; using TUnit.Core;\n"
            + TupleConversionMatrix.Declarations + "\n" + fixture;
        await Verifier.VerifyAnalyzerAsync(source, row.Accepted ? [] :
        [
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
                .WithArguments(row.Source, $"Tuple<{row.Target}>"),
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(1)
                .WithArguments(row.Source, $"Tuple<{row.Target}>")
        ]);
    }

    [Test]
    public async Task RuntimeFixturesStayInSync()
    {
        var expected = TupleConversionMatrix.RuntimeSource().Replace("\r\n", "\n");
        var path = Path.Combine(AppContext.BaseDirectory, "ConversionFixtures.cs.txt");
        var actual = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
        if (expected != actual)
        {
            await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "ConversionFixtures.received.txt"), expected);
        }
        await TUnit.Assertions.Assert.That(actual).IsEqualTo(expected);
    }
}
