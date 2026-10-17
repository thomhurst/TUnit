namespace TUnit.Core.SourceGenerator.Tests;

internal class TestFilteringHookTests : TestsBase
{
    [Test]
    public Task Test() => HooksGenerator.RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "TestFilteringHookTests.cs"),
        async generatedFiles =>
        {
        });
}
