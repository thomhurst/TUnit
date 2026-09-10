
namespace TUnit.Core.SourceGenerator.Tests;

internal class GlobalStaticBeforeEachTests : TestsBase
{
    [Test]
    public Task Test() => HooksGenerator.RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeEveryTests.cs"),
        async generatedFiles =>
        {
            });
}
