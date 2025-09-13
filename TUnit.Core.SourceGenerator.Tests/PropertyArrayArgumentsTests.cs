namespace TUnit.Core.SourceGenerator.Tests;

internal class PropertyArrayArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "PropertyArrayArgumentsTests.cs"),
        async generatedFiles =>
        {
            // Verify that the generated code properly handles array properties
            await Verify(generatedFiles);
        });
}