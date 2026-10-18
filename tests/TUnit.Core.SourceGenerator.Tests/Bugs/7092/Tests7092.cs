namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7092 : TestsBase
{
    // SkipIfEmpty on [MethodDataSource] must be copied onto the generated attribute.
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7092",
        "SkipIfEmptyMethodDataSourceTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Contains(file => file.Contains("SkipIfEmpty = true,"));
        });
}
