
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1304;

internal class Tests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1304",
            "Tests.cs"),
        async generatedFiles =>
        {
            });
}
