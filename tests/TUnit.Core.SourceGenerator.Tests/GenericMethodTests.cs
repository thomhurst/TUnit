
namespace TUnit.Core.SourceGenerator.Tests;

internal class GenericMethodTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "GenericMethodTests.cs"),
        async generatedFiles =>
        {
            });
}
