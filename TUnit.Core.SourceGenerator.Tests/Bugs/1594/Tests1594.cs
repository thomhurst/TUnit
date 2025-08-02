
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1594;

internal class Tests1594 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1594",
            "MyTests.cs"),
        async generatedFiles =>
        {
            });
}
