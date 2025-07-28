namespace TUnit.Core.SourceGenerator.Tests;

internal class BasicTests : TestsBase
{
    [Test]
    public Task Test() => TestMetadataGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        async generatedFiles =>
        {
            });
}
