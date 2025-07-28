
namespace TUnit.Core.SourceGenerator.Tests;

internal class TestDiscoveryHookTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TestDiscoveryHookTests.cs"),
        async generatedFiles =>
        {
            });
}
