
namespace TUnit.Core.SourceGenerator.Tests.Bugs._2083;

internal class Tests2083 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2083",
            "Tests.cs"),
        async generatedFiles =>
        {
            });
}
