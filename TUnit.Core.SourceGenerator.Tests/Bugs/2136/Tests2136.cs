
namespace TUnit.Core.SourceGenerator.Tests.Bugs._2136;

internal class Tests2136 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2136",
            "Tests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
