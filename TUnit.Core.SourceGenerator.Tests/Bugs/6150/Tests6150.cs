
namespace TUnit.Core.SourceGenerator.Tests.Bugs._6150;

internal class Tests6150 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "6150",
            "Tests.cs"),
        async generatedFiles =>
        {
            });
}
