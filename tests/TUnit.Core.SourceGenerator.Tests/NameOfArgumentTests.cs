
namespace TUnit.Core.SourceGenerator.Tests;

internal class NameOfArgumentTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "NameOfArgumentTests.cs"),
        async generatedFiles =>
        {
            });
}
