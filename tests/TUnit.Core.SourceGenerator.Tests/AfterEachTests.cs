
namespace TUnit.Core.SourceGenerator.Tests;

internal class AfterTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        async generatedFiles =>
        {
            });
}
