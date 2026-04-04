
namespace TUnit.Core.SourceGenerator.Tests.Bugs._5118;

internal class Tests5118 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "5118",
            "AsyncClassMethodDataSourceTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
        });
}
