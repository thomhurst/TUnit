
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1589;

internal class Tests1589 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1589",
            "MyTests.cs"),
        async generatedFiles =>
        {
            });
}
