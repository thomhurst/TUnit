
namespace TUnit.Core.SourceGenerator.Tests;

internal class GlobalStaticAfterEachTests : TestsBase
{
    [Test]
    public Task Test() => HooksGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterEveryTests.cs"),
        async generatedFiles =>
        {
            });
}
