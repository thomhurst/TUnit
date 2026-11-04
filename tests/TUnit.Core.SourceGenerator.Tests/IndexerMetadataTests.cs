namespace TUnit.Core.SourceGenerator.Tests;

internal class IndexerMetadataTests : TestsBase
{
    [Test]
    public Task OmitsIndexersAndPreservesOrdinaryProperties() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7531", "IndexerMetadataTests.cs"),
        async files =>
        {
            var output = string.Join("\n", files);
            await Assert.That(output).DoesNotContain("this[]");
            await Assert.That(output).DoesNotContain("get_Item");
            await Assert.That(output).Contains("Name = \"Label\"");
            await Assert.That(output).Contains("Name = \"Value\"");
        });
}
