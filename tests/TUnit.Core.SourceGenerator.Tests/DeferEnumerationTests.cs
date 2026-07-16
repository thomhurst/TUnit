
namespace TUnit.Core.SourceGenerator.Tests;

internal class DeferEnumerationTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "DeferEnumerationTests",
            "DeferEnumerationTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
            await Assert.That(string.Join("\n", generatedFiles)).Contains("DeferEnumeration = true");
        });
}
