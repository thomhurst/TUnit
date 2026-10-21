namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Snapshot coverage for https://github.com/thomhurst/TUnit/issues/7165: top-level <c>dynamic</c> parameters,
/// constructor parameters and properties are rendered as <c>object</c>, while nested uses such as
/// <c>List&lt;dynamic&gt;</c> and <c>dynamic[]</c> keep their spelling. <see cref="DynamicParameterCompilationTests"/>
/// checks that the generated code compiles.
/// </summary>
internal class Tests7165 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
            Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7165",
            "DynamicParameterTests.cs"),
        async generatedFiles =>
        {
            var source = string.Join(Environment.NewLine, generatedFiles);

            await Assert.That(source).DoesNotContain("typeof(dynamic)");
            await Assert.That(source).Contains("typeof(object)");
        });
}
