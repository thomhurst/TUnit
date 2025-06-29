
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1821;

internal class Tests1821 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1821",
            "Tests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
