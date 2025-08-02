
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1539;

internal class Tests1539 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1539",
            "Tests.cs"),
        async generatedFiles =>
        {
            });
}
