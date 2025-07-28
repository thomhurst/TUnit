
namespace TUnit.Core.SourceGenerator.Tests.Bugs._2085;

internal class Tests2085 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2085",
            "Tests.cs"),
        async generatedFiles =>
        {
            });
}
