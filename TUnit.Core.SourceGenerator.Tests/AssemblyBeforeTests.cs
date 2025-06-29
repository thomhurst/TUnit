
namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyBeforeTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "AssemblyBeforeTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
