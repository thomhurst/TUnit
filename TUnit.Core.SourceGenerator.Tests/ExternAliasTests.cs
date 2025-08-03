namespace TUnit.Core.SourceGenerator.Tests;

internal class ExternAliasTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ExternAliasTests.cs"),
        async generatedFiles =>
        {
            // Test that extern alias is respected in generated code
        });
}