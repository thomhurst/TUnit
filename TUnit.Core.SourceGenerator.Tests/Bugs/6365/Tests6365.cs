
namespace TUnit.Core.SourceGenerator.Tests.Bugs._6365;

internal class Tests6365 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "6365",
            "Tests.cs"),
        async generatedFiles =>
        {
        });
}
