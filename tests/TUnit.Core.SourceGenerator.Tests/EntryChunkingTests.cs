namespace TUnit.Core.SourceGenerator.Tests;

internal class EntryChunkingTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "EntryChunkingTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
