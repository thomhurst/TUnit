
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1432;

internal class ConstantInBaseClassTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1432",
            "ConstantInBaseClassTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}
