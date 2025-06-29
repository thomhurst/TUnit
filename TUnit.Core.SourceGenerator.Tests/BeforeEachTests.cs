
namespace TUnit.Core.SourceGenerator.Tests;

internal class BeforeTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
