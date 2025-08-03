using TUnit.Core.Executors;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NumberArgumentTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NumberArgumentTests.cs"),
        async generatedFiles =>
        {
            });

    [Test]
    [Culture("de-DE")]
    public Task TestDE() => Test();
}
